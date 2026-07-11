// APIでエラーを返すときの共通の形です。
// 文字列だけを返すより、code と message を分けるとクライアント側で扱いやすくなります。
public record ApiError(
    string Code,
    string Message
);
