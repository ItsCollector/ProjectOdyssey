namespace ProjectOdyssey
{
    public readonly struct Result<T>
    {
        public bool isSuccess { get; }
        public T? value { get; }
        public string? error { get; }
        
        private Result(bool isSuccess, T? value, string? error)
        {
            this.isSuccess = isSuccess;
            this.value = value;
            this.error = error;
        }

        public static Result<T> Ok(T value) => new Result<T>(true, value, null);
        public static Result<T> Err(string error) => new Result<T>(false, default, error);
    }
}
