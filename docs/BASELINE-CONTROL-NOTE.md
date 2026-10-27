# Runtime baseline flag verification

Revision104 changes only the shared `--hd2d-baseline` lookup from managed process arguments to Godot user arguments. The embedded runtime diagnostic in revision94 reported managedArgs=[] despite the supplied Godot flag. Default enabled behavior is unchanged. A fresh explicit negative control is required for104; compilation alone is not proof.

The original94 Fang baseline/enabled comparison was invalid: both arms were enabled. Revision96 corrected that isolated flag and proved actual receiver-only differences, but Fang remained visually unselected and is absent from this branch.

Historical comparisons depending on an unobserved managed-argument baseline flag must not be treated as proven disabled. Separate immutable source-to-source comparisons are different evidence; review their actual source hashes/receipts rather than assuming every directory named baseline used a flag. The retained top-level QA shell scripts at this audit had no global --hd2d-baseline invocation. This bounded search does not certify unavailable historical runs.
