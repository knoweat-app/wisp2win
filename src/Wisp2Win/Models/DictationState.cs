namespace Wisp2Win.Models;

public enum DictationState
{
    Idle,
    Recording,
    DownloadingModel,
    Transcribing,
    Inserting,
    Error
}
