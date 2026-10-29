# Unity port start HEAD versus source-port checkpoint

The dedicated branch `codex/unity-checkpoint305-20261029` was created from **05ecb3022b15d841eb9bbf316555f51425d7002f**. Its branch reflog retains `05ecb302 branch: Created from HEAD`.

**5668c3d07b66653dbfb40a8212d0ea0647b7e730** is an ancestor, the commit titled `feat(unity): dot の Godot 枝（checkpoint 192）の続きを Unity へ移植 — HD-2D の攻撃演出と敵の技ごとのコマ`. It identifies the inherited Unity effect-port checkpoint192, not the checkout's starting HEAD. There are28 commits in `5668c3d0..05ecb302`, including UI scene/campfire/shop work. `git merge-base 05ecb302 5668c3d0` returns5668c3d0; `git merge-base --is-ancestor 05ecb302 HEAD` succeeds.

The earlier abbreviated wording “Unity baseline192/5668c3d0” described effect lineage but was ambiguous as checkout provenance. Both identifiers must be stated separately in future reports. This difference is not a rebase or a rollback of user work.

The branch reflog has no rebase/reset. It contains local commits after05ecb302 and amendments of two task-owned, unpublished commits:16d43830→780a90e8 (Cold port) and6f1b9024→1a7844d8 (remove an unintended file-mode change in the owned-contact commit). Original source/main and other workers' changes were not reverted. No merge into main or force push occurred.
