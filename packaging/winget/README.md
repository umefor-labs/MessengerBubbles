# winget

`manifests/` holds the first winget manifest for `umefor-labs.MessengerBubbles` 1.0.0, laid out the way [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) expects.

## First submission (once)

On Windows:

```powershell
winget install Microsoft.WingetCreate
wingetcreate submit packaging\winget\manifests\u\umefor-labs\MessengerBubbles\1.0.0
```

`wingetcreate` asks you to sign in to GitHub in the browser, forks winget-pkgs to your account and opens the pull request. Microsoft's bots validate it and a moderator merges it, usually within a few days.

## Later releases

`.github/workflows/release.yml` has a `winget` job that sends each new release to winget with `wingetcreate update`. It runs only when the repository has a `WINGET_TOKEN` secret: a classic personal access token with the `public_repo` scope, from the account that owns the winget-pkgs fork. The job only works after the first submission has been merged.
