# ALVR modifié pour LunaQuest

LunaQuest embarque deux parties d'[ALVR](https://github.com/alvr-org/ALVR) **v20.14.1**, modifiées :

- **LunaQuest Client** (`LunaQuest-Client.apk`) : l'appli du casque, aux couleurs de LunaQuest ;
- **la partie PC** (dossier `ALVR` installé avec LunaQuest) : le pilote SteamVR, avec les modes de suivi du corps FBE.

Ce dossier contient tout ce qu'il faut pour les recompiler soi-même.

## Ce qui change par rapport à ALVR

| Fichier | Modification |
|---|---|
| `alvr/client_openxr/Cargo.toml` | Nom de paquet `alvr.client.lunaquest`, nom affiché « LunaQuest » |
| `alvr/client_openxr/resources/…` | Icône lune sur fond nuit |
| `alvr/client_core/src/storage.rs` | Nom de casque fixe `lunaquest.client`, pour que LunaQuest l'enregistre tout seul |
| `alvr/client_openxr/src/lib.rs` | Espace couleur natif des casques Meta (Rift CV1) au lieu de Rec.709 : couleurs plus vives |
| `alvr/client_openxr/src/lobby.rs` | Active le décor du salon d'attente |
| `alvr/graphics/resources/lobby_quad.wgsl` | Ciel étoilé, lune et sol lunaire |
| `alvr/graphics/src/lobby.rs` | Panneau d'accueil LunaQuest (titre, lune, messages) |
| `alvr/server_openvr/cpp/alvr_server/LunaBody.*` | Suivi du corps de LunaQuest : trackers virtuels au choix, décalages, mode FBE Mix (bêta) qui estime torse, coudes et genoux à partir du casque, des manettes et des vrais trackers, avec calibration |
| `alvr/server_openvr/cpp/alvr_server/alvr_server.cpp` | Branche LunaBody dans le pilote (réglages lus dans `%APPDATA%\LunaQuest\body.json`, écrits par LunaQuest) |
| `alvr/xtask/src/dependencies.rs` | Versions d'outils verrouillées pour compiler sous Windows |
| `Cargo.toml` | Version publique sans infos de débogage |

Tout est dans [`lunaquest-alvr.patch`](lunaquest-alvr.patch). Sans `body.json`, le pilote garde le comportement d'origine d'ALVR.

## Récupérer les sources

```bash
git clone --branch v20.14.1 --depth 1 https://github.com/alvr-org/ALVR.git
cd ALVR
git submodule update --init --depth 1
git apply ../lunaquest-alvr.patch
```

## Recompiler la partie PC

Il faut : Rust 1.88 (toolchain MSVC), Visual Studio Build Tools (C++) avec Clang/LLVM, et `LIBCLANG_PATH` qui pointe vers le dossier `bin` de LLVM.

```bash
cargo xtask prepare-deps --platform windows --no-nvidia
cargo xtask build-streamer --release
```

Le résultat est dans `build/alvr_streamer_windows`.

## Recompiler l'appli du casque

Il faut : Rust 1.88, Android NDK 26.3.11579264, Android SDK (plateforme 32, build-tools 34.0.0) et un JDK 17.

`dependencies.rs` installe `cargo-apk` depuis une copie locale (`../tools/cargo-apk/cargo-apk`) :
clone [zarik5/cargo-apk](https://github.com/zarik5/cargo-apk) à cet endroit et remplace son `Cargo.lock`
par [`cargo-apk.Cargo.lock`](cargo-apk.Cargo.lock), puis :

```bash
cargo xtask prepare-deps --platform android
cargo xtask build-client --release
```

L'APK est créé dans `build/alvr_client_android/alvr_client_android.apk`.

## Licence

ALVR est sous licence MIT (Copyright (c) 2018-2019 polygraphene, Copyright (c) 2020-2024 alvr-org).
Les modifications de LunaQuest sont sous la même licence. Voir [`THIRD-PARTY-NOTICES.txt`](../res/THIRD-PARTY-NOTICES.txt).
