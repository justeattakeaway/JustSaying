## About Versioning

JustSaying adopts [SemVer 2.0](https://semver.org/spec/v2.0.0.html), and uses [MinVer](https://github.com/adamralph/minver) to achieve this. [See here](https://github.com/adamralph/minver#how-it-works) for more information on how it works.

## Process for Releasing

To create a new release go to the GitHub [releases tab](https://github.com/justeat/JustSaying/releases) and click `Draft a new release`.

The tag should be in the format:

`v{MAJOR}.{MINOR}.{PATCH}`

For example `v7.0.0`.

A GitHub Actions build will queue automatically when the tag is created, and publish the packages to NuGet.org.

## Example Release Pattern

Here is what the versioning should typically look like, including what and when to tag a release as outlined above.

- 7.0.0-alpha.0.49 (49 commits above the last tag or root commit)
- 7.0.0-alpha.0.50
- 7.0.0-alpha.0.51
- ...
- 7.0.0-alpha.1 (tagged)
- 7.0.0-alpha.1.1
- 7.0.0-alpha.1.2
- ...
- 7.0.0-alpha.2 (tagged)
- 7.0.0-alpha.2.1
- 7.0.0-alpha.2.2
- ...
- 7.0.0-beta.1 (tagged)
- 7.0.0-beta.1.1
- 7.0.0-beta.1.2
- ...
- 7.0.0 (tagged)
- 7.0.1-alpha.0.1 (default pre-release identifiers)
- 7.0.1-alpha.0.2

(Credit to @adamralph)
