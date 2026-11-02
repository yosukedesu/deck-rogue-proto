# Exact-three attack material recordings — 2026-10-25

Status: numerical/metadata-guided candidates, NOT listened to. No product source edits. No license purchases, accounts, terms acceptance, external uploads or approvals. All masters preserved unmodified. Candidate edits reproducible with prepare_candidates.py. Relative paths below are under `/workspace/scratch/948944000d55/audio-foley-20261025/`.

## Rights and provenance

All selected material explicitly CC0; attribution not required but retained. CC0 1.0: https://creativecommons.org/publicdomain/zero/1.0/

|Material|Creator official source|Download actually used|License evidence / local masters|
|---|---|---|---|
|Impact Sounds 1.0|Kenney — https://kenney.nl/assets/impact-sounds|https://kenney.nl/media/pages/assets/impact-sounds/87b4ddecda-1677589768/kenney_impact-sounds.zip|Official page CC0; `kenney-impact/License.txt` permits commercial use. `kenney-impact/Audio/`. Official Foley tag; exact recording process not stated.|
|RPG Audio|Kenney Vleugels — https://kenney.nl/assets/rpg-audio|https://kenney.nl/media/pages/assets/rpg-audio/8e99002d76-1677590336/kenney_rpg-audio.zip|Official page CC0; `kenney-rpg/License.txt` permits commercial use. `kenney-rpg/Audio/`. Official Foley tag; exact recording process not stated.|
|Bamboo swishes|qubodup / Iwan Gabovitch creator submission — https://opengameart.org/content/swish-bamboo-stick-weapon-swhoshes|https://opengameart.org/sites/default/files/swoshes.7z|Page CC0 and explicitly describes waving bamboo stick at microphone. `qubodup-swish/swoosh.txt` public domain / CC0 / any use statement. `qubodup-swish/*.flac`.|
|Fire Crackling|AntumDeluge creator submission — https://opengameart.org/content/fire-crackling|https://opengameart.org/sites/default/files/fire-1.wav|Page CC0 and creator's public-domain confirmation. Described as popping/crackling fireplace; no detailed recording setup claimed. `antumdeluge-fire/fire-1.wav`.|
|Fall of Stone #1022|Joseph SARDIN official — https://bigsoundbank.com/chute-de-pierre-s1022.html|https://bigsoundbank.com/UPLOAD/bwf-en/1022.wav|Exact page CC0. https://bigsoundbank.com/licenses.html explicitly CC0 1.0, remix/share/commercial allowed. Recording 1–3 kg stones falling 50 cm onto stone pile, Tascam DR-40 + Sennheiser ME66. `bigsoundbank-stone/1022.wav`.|
|Gasoline fire #3 #1342|Joseph SARDIN official — https://bigsoundbank.com/gasoline-fire-3-s1342.html|https://bigsoundbank.com/UPLOAD/bwf-en/1342.wav|Exact page CC0; same official license. Recorded gasoline ignition, MixPre-3 + ME66, 48kHz 24bit mono. `bigsoundbank-fire/1342.wav`.|

Downloaded source HTML and license HTML are retained in `provenance/`. Binary SHA256s and decoded properties in `metadata.json`; selected master and derived hashes, precise cuts, gain and output peaks in `candidate_manifest.json`.

Suggested voluntary credits: Additional sounds: Kenney (kenney.nl), qubodup / Iwan Gabovitch, AntumDeluge, Joseph SARDIN (BigSoundBank.com), licensed CC0 1.0.

## Candidate files and integration plan (engineer owns actual mix)

All candidate files: 48kHz mono PCM24, 2ms entrance fade, 20ms ending fade, no pitch shifting or time stretch. Source mono/stereo conversion is recorded; mono sum may have different peaks than original channels. The prepared candidate output peaks below are checked. Masters remain authoritative.

- green_strike onset: `candidates/axe_whoosh_bamboo02.wav` (0.202s, -12.24dBFS peak), alternate bamboo27 (0.213s, -12.25dBFS). These fit 0.22s lead. Material origin is explicitly recorded bamboo, not generated noise.
- green_strike contact: `candidates/axe_wood_contact.wav` (0.310s, -6.94dBFS), optional low-level `axe_chop_contact.wav` (0.223s, -6.17dBFS). Use one dry dominant contact, not two equally loud attacks. Add existing low body only to fill low band, no resonant metal bell.
- red_strike: `candidates/fire_ignition.wav` (0.55s, -12.70dBFS) is original #1342 window 0.30–0.85s; onset/pressure texture can be fitted to the 0.28s lead and 0.55s contact with distinct envelopes, not duplicated loud transient. `candidates/fire_crackle.wav` (0.55s, -10.03dBFS) is fireplace 0.65–1.20s, to be quieter decay texture (start after pressure hit). Exact perceptual ignition/contact selection remains audition-dependent.
- green_finisher_stomp contact: `candidates/ground_soft_thud.wav` (0.527s,-6.92dBFS) for dry mass + subtle existing low tone. `stone_debris_a.wav` or `stone_debris_b.wav` (0.65s, -6.32/-7.90dBFS peaks) for real stone movement, starting ~30–70ms after sole/ground contact and lower than body. No metallic chime or continuous generic hiss.
- Keep existing authoritative 2 events per card, each once; no extra damage/event trigger. Fits existing starts/contact C=.32/.40/.62. Don’t make all cues louder to simulate quality. Normalize final mixed clip, not each layer independently, retain 3–6dB peak headroom; verify captured combined mix.

## Numerical caveats / remaining QA

`chop.ogg` original Vorbis float decoding exceeds full scale in some samples; positive decoded samples are not proof of source clipping but require gain before integer PCM encoding. Stereo-to-mono conversion can increase amplitude. Prepared outputs remain below full scale. Long stone/fire masters include full-scale samples outside selected windows; stone windows 4.60–5.25 and 6.17–6.82, ignition 0.30–0.85 have no samples at full scale by 48kHz mono float check. Do not use arbitrary whole master as an attack cue.

No audible quality, absence of speech/noise, or subjective superiority has been established. Filenames, creator descriptions, sample envelopes and rights are verified; actual game capture can establish synchronization and clipping, not substitute for listening. These are candidates, not certification of 'highest quality'.
