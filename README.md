# Seq.App.Relay&nbsp;[![NuGet Version](https://img.shields.io/nuget/v/Seq.App.Relay.svg?style=flat)](https://www.nuget.org/packages/Seq.App.Relay/)

This plug-in [Seq app](https://docs.datalust.co/docs/installing-output-apps) forwards logs and traces from one Seq server to another.

## Compared with Seq.App.Replication

_Seq.App.Replication_ is an earlier implementation of the same functionality.

* This app, _Seq.App.Relay_, supports replicating logs and traces, while _Seq.App.Replication_ only supports logs
* This app supports Seq's OpenTelemetry envelope properties like `@Resource` and `@Scope`, while _Seq.App.Replication_ does not
* _Seq.App.Replication_ supports durable disk buffering, while this app uses an in-memory buffer only
