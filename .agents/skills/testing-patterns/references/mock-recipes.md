# Mocking Recipes

### The UnitOfWork Pitfall
When writing services, `ExecuteTransactionAsync` takes a `Func<Task>` delegate. If you do not execute the delegate inside the mock, the internal transaction body will mathematically "never run". This leads to green tests that don't actually cover your lambda.

```csharp
// ❌ WRONG (Returns successfully, but does not execute the action):
_unitOfWork.Setup(u => u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), Ct))
           .Returns(Task.CompletedTask);

// ✅ CORRECT:
_unitOfWork.Setup(u => u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), Ct))
           .Returns<Func<Task>, CancellationToken>((action, _) => action());
```

### Call Count Validation
Use Moq's `Times` struct.
```csharp
// Assert action happened
_repo.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Once);

// Assert safety mechanism (short-circuited)
_repo.Verify(r => r.DeleteAsync(It.IsAny<Product>()), Times.Never);
```
