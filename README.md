# Pull request size and stability labels

This action adds size and code stability labels to pull requests.

![](img.png)

PR additions  | Label
--------------|-----------
0-24          | extra small
25-74         | small
75-149        | medium
150+          | large

Code stability is evaluated for each contributor.

Stability rating  | Label
------------------|----------------
<1379             | stability/low
1379-1621         | stability/medium
\>1621            | stability/high

When a contributor deletes lines of code, he increases his rating and lowers the rating of the deleted lines author. Elo rating system is used. [More...](https://github.com/victorx64/devrating)

## Usage

Throw this to `.github/workflows/pr-label.yml` in your repo:

```yaml
name: Update PR labels
on:
  pull_request:
    types: [ opened, synchronize ]
  push:
    branches: [ master, main ]
jobs:
  update-pr-labels:
    runs-on: ubuntu-latest
    steps:
    - uses: victorx64/pr-label@v0
```
