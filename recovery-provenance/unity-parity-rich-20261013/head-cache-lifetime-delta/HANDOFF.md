# Enemy-head cache lifetime guard

Off-tree follow-up to the accepted geometry-preserving head alpha cache. Presentation-only; no pixel or gameplay change expected.

- Capacity512 distinct (Texture.GetInstanceId(), PhoneLayout) keys, scalar float values and scalar FIFO keys only. Cache holds no Texture2D or Image references.
- Hit path is unchanged: dictionary lookup + cache-hit counter, no allocation/readback/queue mutation. On a new miss, FIFO eviction bounds both collections.
- Existing using-var Image lifetime, alpha threshold16/255 and exact SourceEnemyUiAnchor.Height policy are unchanged.
- Explicit run/session reset via existing UxResetHand and scene-tree exit via existing bound _ExitTree clear both collections. Ordinary Changed/Rebuild does not clear them.
- Lifetime counters stay cumulative. Passive new probes: EnemyHeadCacheEntries, EnemyHeadCacheEvictions. Reset starts a new warmup, never an every-frame warmup.

Native gate after the current pointer window: same pixels before/after; native entry count<=512; repeated idle frames/rebuilds retain entries and add hits without new readbacks; explicit reset starts with empty collections and repopulates once per actual texture/phone key. Adversarial512+key eviction is pure tested, not a requested native texture-readback stress.
