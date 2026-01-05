# Commands + Streaming Model

## Per-repo command processor
- IRepoCommand -> IAsyncEnumerable<RepoEvent>

RepoEvent kinds:
- ProgressEvent (stage, message, percent?)
- DataEvent<T> (payload)
- CompletedEvent (summary)
- FailedEvent (error contract)

Implementation approach:
- Use System.Threading.Channels
- Expose NDJSON streaming from API:
  - content-type application/x-ndjson
  - one JSON object per line

Concurrency:
- Serialize load/compile operations per repo
- Allow concurrent read queries if safe
