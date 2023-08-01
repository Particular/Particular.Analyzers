# Particular.Analyzers

This project contains Roslyn analyzers that are used by the team at [Particular Software](https://particular.net).

## CI

This project's CI is in GitHub Actions. All pull requests are built and tested by the CI.

## Deployment

Tagged versions are automatically pushed to [feedz.io](https://feedz.io/org/particular-software/repository/packages/packages/Particular.Analyzers). After validating new versions, the package should be promoted to production by pushing the package to NuGet using the feedz.io push upstream feature.
