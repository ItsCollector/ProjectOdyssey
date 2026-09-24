using System.Diagnostics.CodeAnalysis;

namespace ProjectOdyssey.Common
{
    public readonly struct Result<T>
    {
        [MemberNotNullWhen(true, nameof(Value))]
        public bool IsSuccess { get; }

        [MaybeNull]
        public T Value { get; }

        public string Error { get; }

        private Result(bool isSuccess, [AllowNull] T value, string error)
        {
            this.IsSuccess = isSuccess;
            this.Value = value!;
            this.Error = error;
        }

        public static Result<T> Ok(T value) => new Result<T>(true, value, string.Empty);
        public static Result<T> Err(string error) => new Result<T>(false, default, error);

        public bool TryGetValue([MaybeNullWhen(false)] out T value)
        {
            value = this.Value;
            return IsSuccess;
        }

        public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onError)
            => IsSuccess ? onSuccess(Value) : onError(Error);
    }
}