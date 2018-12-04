# Process for Releasing

To create a new release, either push a tag via git, or go to the GitHub [releases tab](https://github.com/justeat/JustSaying/releases) and click `Draft a new release`.

The tag should be in the format:

`v{MAJOR}.{MINOR}.{PATCH}`

For example `v6.0.1`.

Once the AppVeyor build has completed, you can deploy to nuget via the AppVeyor UI.