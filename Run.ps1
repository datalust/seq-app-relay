param($WebhookUrl)

echo "run: Publishing app binaries"

& dotnet publish "$PSScriptRoot/src/Seq.App.Relay" -c Release -o "$PSScriptRoot/src/Seq.App.Relay/obj/publish" --version-suffix=local

if($LASTEXITCODE -ne 0) { exit 1 }    

echo "run: Piping live Seq logs to the app"

& seqcli tail --json | `
    & seqcli app run -d "$PSScriptRoot/src/Seq.App.Relay/obj/publish" `
        -p TargetUrl=http://localhost:5341 `
        -p ApiKey=thisistheapikey 2>&1 | `
    & seqcli print
