using System.Diagnostics.CodeAnalysis;

namespace ProjectOdyssey
{
    public readonly struct Result<T>
    {
        [MemberNotNullWhen(true, nameof(value))]
        public bool isSuccess { get; }

        [MaybeNull]
        public T value { get; }

        public string error { get; }

        private Result(bool isSuccess, [AllowNull] T value, string error)
        {
            this.isSuccess = isSuccess;
            this.value = value!;
            this.error = error;
        }

        public static Result<T> Ok(T value) => new Result<T>(true, value, string.Empty);
        public static Result<T> Err(string error) => new Result<T>(false, default, error);

        public bool TryGetValue([MaybeNullWhen(false)] out T value)
        {
            value = this.value;
            return isSuccess;
        }

        public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onError)
            => isSuccess ? onSuccess(value) : onError(error);
    }
}