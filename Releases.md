# Process for Releasing

First before creating a release, make sure that [the version number in `Directory.Build.Props`](https://github.com/justeat/JustSaying/blob/d30543fbfc3cf640835339efbe497466e230f220/Directory.Build.props#L22) matches the major and minor version you are looking to release.

To create a new release go to the GitHub [releases tab](https://github.com/justeat/JustSaying/releases) and click `Draft a new release`.

The tag should be in the format:

`v{MAJOR}.{MINOR}.{PATCH}`

For example `v6.0.1`.

Once the AppVeyor build has completed, you can deploy to nuget via the AppVeyor UI.