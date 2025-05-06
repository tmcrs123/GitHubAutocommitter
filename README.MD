## What

A simple windows service that runs in the background and push a commit to git.

There are many use cases for this, mine was "I want my notes to be synced automatically somewhere and I don't want to pay for something like Obsidian".

## How

1 - Publish the code as a `.exe`;
2 - Register the service with windows: `sc.exe create GitHubAutoCommitService binPath=path/to/.exe start=auto`
3 - If you need to delete the service: `sc.exe delete GitHubAutoCommitService`

Note: you likely need to run these commands as admin