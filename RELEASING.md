# Publishing to NuGet

Releases use the manual **Publish NuGet Packages** GitHub Actions workflow.
Merging a PR does not publish a package or deploy any consuming application.

## Setup

In this repository, open **Settings > Secrets and variables > Actions** and add
a repository secret named `NUGET_API_KEY`. Its NuGet API key must be unexpired and
allow publishing new versions of `ClearDataService`. Secrets configured in a different
repository are not automatically available here. Replace expired keys in GitHub;
do not put them in source or workflow files.

## Release

1. Review and merge the intended source and package version in `ClearDataService/ClearDataService.csproj`.
2. In **Actions**, select **Publish NuGet Packages**, choose **Run workflow**, and
   select `main`. Runs from other branches are skipped.
3. Check the run completes successfully, then verify the version on NuGet before
   updating downstream references.

The workflow uses .NET 10 on Windows, restores and builds the solution in Release,
runs its automated tests, then packs the tested outputs without rebuilding.
Build or test failure prevents publication. Only `ClearDataService` is packed.
The packages are retained as a commit-labelled workflow artifact for seven days.
Existing published versions are skipped; changes require a new package version.
Release runs cannot overlap, and a new trigger does not cancel an active release
(a newer pending run can replace an older pending run).

Pull requests and pushes to `main` run the same restore, build, test and
pack commands through **Validate**, without publishing or accessing the NuGet key.

The interactive `ClearDataService.IntegrationTests` console application is built,
but its live Cosmos DB/SQL Server scenarios are not executed by these workflows.
The test gate runs the automated `ClearDataService.Tests` suite.
