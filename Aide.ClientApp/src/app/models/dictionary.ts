// Pattern for a claim status dictionary: { [id: number]: string }
export interface IDictionary<T> {
    [key: number]: T;
}
