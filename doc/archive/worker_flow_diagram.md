# InventoryAlert.Worker — Detailed Implementation Architecture

This diagram details the specific files and methods involved in the background processing ecosystem.

### 1. High-Detail Flow Diagram

```mermaid
graph TD
    subgraph Host ["Host Layer"]
        PRG[Program.cs] --> |Register| NSW[NativeSqsWorker.cs]
        PRG --> |Register| QHS[QueuedHostedService.cs]
        PRG --> |Register| JSS[JobSchedulerService.cs]
    end

    subgraph NativeFlow ["Path B: Native SQS (Real-time)"]
        NSW --> |ExecuteAsync| PQJ[ProcessQueueJob.cs]
        PQJ --> |ExecuteAsync Loop| SHP[SqsHelper.cs]
        SHP --> |ReceiveMessagesAsync| SQS((Amazon SQS))
        PQJ --> |ProcessBatchAsync| DISP[SqsDispatcher.cs]
    end

    subgraph HangfireFlow ["Path A: Hangfire (Scheduled)"]
        JSS --> |StartAsync| RJG[IRecurringJobManager]
        RJG --> |AddOrUpdate| HFDB[(Hangfire Storage)]
        HFDB --> |Trigger| PSJ[PollSqsJob.cs]
        PSJ --> |ExecuteAsync| SHP
        PSJ --> |ProcessBatchAsync| DISP
    end

    subgraph CoreLogic ["Core Processing Engine"]
        DISP --> |DispatchAsync| DDP[Redis: Atomic Dedup]
        DDP --> |Unique| CLD[Price Alert Cooldown]
        CLD --> |Allow| PROC[MessageProcessor.cs]
        PROC --> |ProcessMessageAsync| HAND[Integration Handlers]
        HAND --> |HandleAsync| RES[(DynamoDB / DB)]
    end

    subgraph Handlers ["Integration Handlers"]
        HAND --- NH[NewsHandler.cs]
        HAND --- PH[PriceAlertHandler.cs]
        HAND --- SLH[StockLowHandler.cs]
    end

    subgraph InMemFlow ["Path C: In-Memory Task Queue"]
        QHS --> |ExecuteAsync| BTQ[IBackgroundTaskQueue]
        BTQ --> |DequeueAsync| WORK[Func Task]
        WORK --> |Invoke| LOG[(Audit Logs / Sync)]
    end

    style Host fill:#f5f5f5,stroke:#333
    style NativeFlow fill:#fff0f5,stroke:#db7093,stroke-width:2px
    style HangfireFlow fill:#e6f3ff,stroke:#4682b4,stroke-width:2px
    style CoreLogic fill:#f0fff0,stroke:#2e8b57,stroke-width:2px
    style MemoryQueue fill:#ffffff,stroke:#666,stroke-dasharray: 5 5
```

### 2. SQS Dispatch Sequence

```mermaid
sequenceDiagram
    participant SQS as Amazon SQS
    participant Job as Job / Poller
    participant Help as SqsHelper
    participant Disp as SqsDispatcher
    participant Redis as Redis / Cache
    participant Proc as MessageProcessor
    participant Final as IntegrationHandler

    Job->>Help: ReceiveMessagesAsync(queueUrl)
    Help->>SQS: SDK: ReceiveMessageAsync
    SQS-->>Help: List<Message>
    Help-->>Job: List<Message>
    
    Job->>Disp: ProcessBatchAsync(messages)
    loop Each Message in Batch
        Disp->>Disp: TryDeserializeEnvelope
        
        alt Deserialize Success
            Disp->>Redis: Atomic Dedup (StringSetAsync)
            alt Is Unique
                opt EventType == MarketPriceAlert
                    Disp->>Redis: Cooldown Check (IsSupressedAsync)
                end
                
                alt Not Supressed
                    Disp->>Proc: ProcessMessageAsync(message)
                    Proc->>Final: HandleAsync(payload)
                    Final-->>Proc: Complete
                    Proc-->>Disp: Handled
                    Disp->>Redis: Set Key Expiration (48h)
                    Disp->>SQS: DeleteMessageAsync (on success)
                end
            end
        else Bad JSON
            Disp-->>SQS: DeleteMessageAsync (Ack & Drop)
        end
    end
```

### Component & Method Registry

| File Name | Method | Purpose |
| :--- | :--- | :--- |
| **NativeSqsWorker.cs** | `ExecuteAsync` | Initiates the native polling implementation during app startup. |
| **ProcessQueueJob.cs** | `ExecuteAsync` | Runs an infinite `while(!ct.IsCancellationRequested)` loop for SQS. |
| **SqsHelper.cs** | `ReceiveMessagesAsync` | Low-level AWS SDK call to fetch messages from an SQS URL. |
| **SqsDispatcher.cs** | `Batch / Dispatch` | Orchestrates batch processing, de-duplication, and cooldown logic. |
| **MessageProcessor.cs** | `ProcessMessageAsync`| Logic for routing a raw SQS body to a typed C# event handler. |
| **JobSchedulerService.cs**| `StartAsync` | One-time registration of recurring Hangfire jobs into persistent storage. |
| **QueuedHostedService.cs** | `ExecuteAsync` | Continuous consumer of the internal `IBackgroundTaskQueue`. |

> [!IMPORTANT]
> **Path B (Native)** is the most efficient for production as it utilizes "Long Polling" (`WaitTimeSeconds=20`), which stays connected to AWS until a message is available, significantly reducing SDK overhead compared to the Hangfire trigger.
