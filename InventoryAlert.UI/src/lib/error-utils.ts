export interface ApiErrorDetail {
    code: string;
    message: string;
    property?: string;
}

export interface ErrorResponse {
    errors: ApiErrorDetail[];
    errorId: string;
    correlationId?: string;
}

/**
 * Extracts a user-friendly error message from the backend ErrorResponse structure.
 * Prefers the first error's message, falls back to a generic string.
 */
export function getErrorMessage(error: any): string {
    if (!error) return "An unexpected error occurred.";

    // If it's already a string, return it
    if (typeof error === 'string') return error;

    // Handle our standard ErrorResponse shape
    const data = error.data as ErrorResponse;
    if (data?.errors && data.errors.length > 0) {
        return data.errors[0].message;
    }

    // Fallback to standard Error object message
    return error.message || "An unexpected error occurred.";
}

/**
 * Extracts the CorrelationId from the error for support/debugging purposes.
 */
export function getCorrelationId(error: any): string | undefined {
    return error?.data?.correlationId || error?.correlationId;
}
