"""distribute.py — Post-Build Distribution Script fuer RemakeSoF.

Zippt einen fertigen Unity-Build, generiert ein SHA256-Manifest
und laedt beides an den AuthServer (FastAPI) hoch.

Build-Typ wird automatisch aus dem Pfad erkannt (Server/ oder Client/).
Kompression: zstandard > LZMA > DEFLATE (Fallback-Kette).

Verwendung::

    python Tools/distribute.py --build-dir Builds/Client/Windows10 --version 1.2.0
    python Tools/distribute.py --build-dir Builds/Server/Windows10 --version 1.2.0
    python Tools/distribute.py --build-dir Builds/Client/Windows10 --version 1.2.0 --api-url https://api.remakesof.com
    python Tools/distribute.py --build-dir Builds/Client/Windows10 --version 1.0.0 --token "<JWT>"
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import sys
import tarfile
import zipfile
from collections.abc import Callable
from typing import Any

try:
    import requests
except ImportError:
    requests = None

try:
    import zstandard as zstd  # type: ignore[import-untyped]
except ImportError:
    zstd = None

DEFAULT_API_URL = "http://localhost:8000"
OUTPUT_DIR = "Builds/_dist"
ZSTD_LEVEL_DEFAULT = 19
ZSTD_LEVEL_ULTRA = 22

# Ordner/Dateien die nicht in die Distribution gehoeren
EXCLUDE_PATTERNS = [
    re.compile(r"BurstDebugInformation_DoNotShip", re.IGNORECASE),
    re.compile(r"\.pdb$", re.IGNORECASE),
]

# Relativer Pfad zum Art-Ordner innerhalb des Builds (Client-only)
ART_DIR_RELATIVE = "Game_Data/Art"


def sha256_file(filepath: str) -> str:
    """Berechnet den SHA256-Hash einer Datei in Chunks (speicherschonend)."""
    h = hashlib.sha256()
    with open(filepath, "rb") as f:
        while True:
            chunk = f.read(8192)
            if not chunk:
                break
            h.update(chunk)
    return h.hexdigest()


def should_exclude(relative_path: str) -> bool:
    """Prueft ob eine Datei von der Distribution ausgeschlossen werden soll."""
    for pattern in EXCLUDE_PATTERNS:
        if pattern.search(relative_path):
            return True
    return False


def detect_build_type(build_dir: str) -> str:
    """Erkennt den Build-Typ (client/server) aus dem Pfadnamen."""
    normalized = build_dir.replace("\\", "/").lower()
    if "/server/" in normalized or normalized.endswith("/server"):
        return "server"
    if "/client/" in normalized or normalized.endswith("/client"):
        return "client"
    return "client"


def generate_manifest(build_dir: str, version: str, build_type: str,
                      path_filter: Callable[[str], bool] | None = None) -> dict[str, Any]:
    """Erzeugt ein Manifest mit SHA256-Hashes und Dateigroessen fuer alle Dateien im Build.
    
    path_filter: Optional. Bekommt den relativen Pfad (forward slashes) und gibt True zurueck
                 wenn die Datei ins Manifest aufgenommen werden soll.
    """
    files: list[dict[str, Any]] = []
    total_size = 0
    excluded_count = 0

    for root, dirs, filenames in os.walk(build_dir):
        dirs[:] = [d for d in dirs if not should_exclude(d)]

        for filename in filenames:
            filepath = os.path.join(root, filename)
            relative = os.path.relpath(filepath, build_dir).replace("\\", "/")
            if should_exclude(relative):
                excluded_count += 1
                continue
            if path_filter is not None and not path_filter(relative):
                excluded_count += 1
                continue
            file_hash = sha256_file(filepath)
            size = os.path.getsize(filepath)
            total_size += size
            files.append({
                "path": relative,
                "sha256": file_hash,
                "size": size,
            })

    print(f"  {len(files)} Dateien erfasst, {excluded_count} ausgeschlossen, Gesamtgroesse: {total_size / (1024 * 1024):.1f} MB")

    return {
        "version": version,
        "build_type": build_type,
        "file_count": len(files),
        "total_size": total_size,
        "files": files,
    }


def create_archive_zstd(build_dir: str, output_path: str, path_filter: Callable[[str], bool] | None = None,
                        zstd_level: int = ZSTD_LEVEL_DEFAULT) -> str:
    """Erstellt ein .tar.zst Archiv (beste Kompressionsrate) und gibt den SHA256-Hash zurueck."""
    assert zstd is not None, "zstandard muss installiert sein"
    cctx = zstd.ZstdCompressor(level=zstd_level, threads=-1)

    with open(output_path, "wb") as fh:
        with cctx.stream_writer(fh) as compressor:
            with tarfile.open(fileobj=compressor, mode="w|") as tar:
                for root, dirs, filenames in os.walk(build_dir):
                    dirs[:] = [d for d in dirs if not should_exclude(d)]
                    for filename in filenames:
                        filepath = os.path.join(root, filename)
                        arcname = os.path.relpath(filepath, build_dir)
                        if should_exclude(arcname):
                            continue
                        relative = arcname.replace("\\", "/")
                        if path_filter is not None and not path_filter(relative):
                            continue
                        tar.add(filepath, arcname=arcname)

    archive_size = os.path.getsize(output_path)
    archive_hash = sha256_file(output_path)
    label = f"zstd-{zstd_level}" + (" ultra" if zstd_level >= 20 else "")
    print(f"  Archiv ({label}): {archive_size / (1024 * 1024):.1f} MB, SHA256: {archive_hash[:16]}...")
    return archive_hash


def create_archive_zip(build_dir: str, output_path: str, path_filter: Callable[[str], bool] | None = None) -> str:
    """Erstellt ein ZIP-Archiv mit LZMA-Kompression (Fallback) und gibt den SHA256-Hash zurueck."""
    with zipfile.ZipFile(output_path, "w", zipfile.ZIP_LZMA) as zf:
        for root, dirs, filenames in os.walk(build_dir):
            dirs[:] = [d for d in dirs if not should_exclude(d)]
            for filename in filenames:
                filepath = os.path.join(root, filename)
                arcname = os.path.relpath(filepath, build_dir)
                if should_exclude(arcname):
                    continue
                relative = arcname.replace("\\", "/")
                if path_filter is not None and not path_filter(relative):
                    continue
                zf.write(filepath, arcname)

    archive_size = os.path.getsize(output_path)
    archive_hash = sha256_file(output_path)
    print(f"  Archiv (LZMA): {archive_size / (1024 * 1024):.1f} MB, SHA256: {archive_hash[:16]}...")
    return archive_hash


def create_archive(build_dir: str, output_dir: str, base_name: str,
                   path_filter: Callable[[str], bool] | None = None, zstd_level: int = ZSTD_LEVEL_DEFAULT) -> tuple[str, str]:
    """Erstellt das bestmoegliche Archiv. Gibt (dateipfad, sha256_hash) zurueck."""
    if zstd is not None:
        output_path = os.path.join(output_dir, base_name + ".tar.zst")
        label = f"Level {zstd_level}" + (" ultra" if zstd_level >= 20 else "")
        print(f"  Verwende zstandard ({label}, multi-threaded) ...")
        archive_hash = create_archive_zstd(build_dir, output_path, path_filter, zstd_level)
    else:
        output_path = os.path.join(output_dir, base_name + ".zip")
        print(f"  zstandard nicht installiert, verwende ZIP/LZMA ...")
        print(f"  Tipp: pip install zstandard  (fuer ~20-40% kleinere Archive)")
        archive_hash = create_archive_zip(build_dir, output_path, path_filter)
    return output_path, archive_hash


def upload_to_server(api_url: str, version: str, build_type: str,
                     archive_path: str, manifest: dict[str, Any], token: str) -> bool:
    """Laedt Archiv + Manifest per Chunked Streaming an den FastAPI AuthServer hoch."""
    if requests is None:
        print("FEHLER: 'requests' ist nicht installiert. Installiere mit: pip install requests")
        print(f"  Dateien liegen bereit in: {os.path.dirname(archive_path)}")
        return False

    headers = {"Authorization": f"Bearer {token}"}
    archive_hash = sha256_file(archive_path)
    total_size = os.path.getsize(archive_path)

    # 1. Upload initialisieren
    print(f"  Init Upload: {os.path.basename(archive_path)} ({total_size / (1024 * 1024):.1f} MB) ...")
    resp = requests.post(f"{api_url}/api/game/upload/init", headers=headers, json={
        "version": version,
        "build_type": build_type,
        "filename": os.path.basename(archive_path),
        "total_size": total_size,
        "sha256": archive_hash,
    }, timeout=30)

    if resp.status_code != 200:
        print(f"  Init fehlgeschlagen: {resp.status_code} — {resp.text}")
        return False

    upload_id = resp.json()["upload_id"]
    chunk_size = resp.json()["chunk_size"]

    # 2. Chunks senden (mit Resume-Support)
    print(f"  Sende {total_size // chunk_size + 1} Chunks a {chunk_size // (1024 * 1024)} MB ...")
    with open(archive_path, "rb") as f:
        chunk_index = 0
        sent = 0
        while True:
            chunk = f.read(chunk_size)
            if not chunk:
                break
            resp = requests.put(
                f"{api_url}/api/game/upload/{upload_id}/chunk/{chunk_index}",
                headers={**headers, "Content-Type": "application/octet-stream"},
                data=chunk,
                timeout=120,
            )
            if resp.status_code != 200:
                print(f"\n  Chunk {chunk_index} fehlgeschlagen: {resp.status_code} — {resp.text}")
                return False
            sent += len(chunk)
            progress = sent / total_size
            print(f"\r  Upload: {progress:.1%} ({sent / (1024 * 1024):.0f}/{total_size / (1024 * 1024):.0f} MB)", end="", flush=True)
            chunk_index += 1
    print()

    # 3. Manifest hochladen + Finalize
    print(f"  Finalize ...")
    resp = requests.post(
        f"{api_url}/api/game/upload/{upload_id}/finalize",
        headers=headers,
        json={"manifest": manifest},
        timeout=60,
    )

    if resp.status_code != 200:
        print(f"  Finalize fehlgeschlagen: {resp.status_code} — {resp.text}")
        return False

    result = resp.json()
    print(f"  Upload abgeschlossen: {result.get('filename', '?')} — SHA256 verifiziert")
    return True


def build_package(build_dir: str, output_dir: str, version: str,
                  build_type: str, label: str, path_filter: Callable[[str], bool] | None = None,
                  zstd_level: int = ZSTD_LEVEL_DEFAULT) -> tuple[dict[str, Any], str]:
    """Erzeugt Manifest + Archiv fuer ein Paket. Gibt (manifest, archive_path) zurueck."""
    base_name = f"RemakeSoF-{label}-{version}"
    manifest_path = os.path.join(output_dir, f"manifest-{label}-{version}.json")

    print(f"\n--- Paket: {label} ---")
    print(f"  Manifest generieren ...")
    manifest = generate_manifest(build_dir, version, build_type, path_filter)

    print(f"  Archiv erstellen ...")
    archive_path, archive_hash = create_archive(build_dir, output_dir, base_name, path_filter, zstd_level)
    archive_name = os.path.basename(archive_path)

    manifest["archive"] = {
        "filename": archive_name,
        "sha256": archive_hash,
        "size": os.path.getsize(archive_path),
    }

    with open(manifest_path, "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=2)
    print(f"  Manifest gespeichert: {manifest_path}")

    return manifest, archive_path


def main():
    parser = argparse.ArgumentParser(
        description="RemakeSoF Build Distribution — ZIP + Manifest + Upload"
    )
    parser.add_argument(
        "--build-dir", required=True,
        help="Pfad zum fertigen Build-Ordner (z.B. Builds/Client/Windows10)"
    )
    parser.add_argument(
        "--version", required=True,
        help="Versionsnummer (z.B. 1.2.0)"
    )
    parser.add_argument(
        "--build-type", default=None, choices=["client", "server"],
        help="Build-Typ: client oder server (default: auto-detect aus Pfad)"
    )
    parser.add_argument(
        "--api-url", default=DEFAULT_API_URL,
        help=f"URL des AuthServers (default: {DEFAULT_API_URL})"
    )
    parser.add_argument(
        "--token",
        help="Admin JWT-Token fuer den Upload. Ohne Token wird nur lokal gepackt."
    )
    parser.add_argument(
        "--no-upload", action="store_true",
        help="Nur ZIP + Manifest erstellen, nicht hochladen."
    )
    parser.add_argument(
        "--ultra", action="store_true",
        help=f"Maximale Kompression (zstd Level {ZSTD_LEVEL_ULTRA} statt {ZSTD_LEVEL_DEFAULT}). Langsamer, ~2-3%% kleiner."
    )
    parser.add_argument(
        "--output-dir", default=OUTPUT_DIR,
        help=f"Ausgabe-Ordner fuer ZIP + Manifest (default: {OUTPUT_DIR})"
    )

    args = parser.parse_args()

    build_dir = os.path.abspath(args.build_dir)
    if not os.path.isdir(build_dir):
        print(f"FEHLER: Build-Ordner existiert nicht: {build_dir}")
        sys.exit(1)

    build_type = args.build_type or detect_build_type(build_dir)
    zstd_level = ZSTD_LEVEL_ULTRA if args.ultra else ZSTD_LEVEL_DEFAULT
    print(f"Build-Typ: {build_type} ({'auto-detect' if args.build_type is None else 'manuell'})")
    print(f"Kompression: zstd Level {zstd_level}{' (ultra)' if args.ultra else ''}")

    output_dir = os.path.abspath(args.output_dir)
    os.makedirs(output_dir, exist_ok=True)

    art_prefix = ART_DIR_RELATIVE + "/"
    has_art_dir = build_type == "client" and os.path.isdir(os.path.join(build_dir, ART_DIR_RELATIVE))

    if has_art_dir:
        # Client-Build: aufteilen in client (ohne Art/) + assets (nur Art/)
        print(f"\nClient-Build erkannt — trenne Art-Assets ({ART_DIR_RELATIVE}/) ab ...")

        # 1. Client-Paket (ohne Art/)
        client_filter: Callable[[str], bool] = lambda p: not p.startswith(art_prefix)
        client_manifest, client_archive = build_package(
            build_dir, output_dir, args.version, build_type, "client", client_filter, zstd_level)

        # 2. Assets-Paket (nur Art/)
        assets_filter: Callable[[str], bool] = lambda p: p.startswith(art_prefix)
        assets_manifest, assets_archive = build_package(
            build_dir, output_dir, args.version, "assets", "assets", assets_filter, zstd_level)

        packages = [
            ("client", client_manifest, client_archive),
            ("assets", assets_manifest, assets_archive),
        ]
    else:
        # Server-Build oder Client ohne Art/: ein Paket
        manifest, archive = build_package(
            build_dir, output_dir, args.version, build_type, build_type, zstd_level=zstd_level)
        packages = [(build_type, manifest, archive)]

    # Upload
    if args.no_upload:
        print(f"\nUpload uebersprungen (--no-upload).")
        print(f"Fertig! Dateien liegen in: {output_dir}")
        return

    if not args.token:
        print(f"\nKein --token angegeben, Upload uebersprungen.")
        print(f"Fertig! Dateien liegen in: {output_dir}")
        print(f"  Manueller Upload: POST {args.api_url}/api/game/upload/init")
        return

    print(f"\nUpload an {args.api_url} ({len(packages)} Paket(e)) ...")
    all_ok = True
    for pkg_type, pkg_manifest, pkg_archive in packages:
        print(f"\n--- Upload: {pkg_type} ---")
        success = upload_to_server(
            args.api_url, args.version, pkg_type,
            pkg_archive, pkg_manifest, args.token
        )
        if not success:
            all_ok = False
            print(f"  Upload von {pkg_type} fehlgeschlagen!")

    if all_ok:
        print("\nDistribution abgeschlossen!")
    else:
        print("\nEinige Uploads fehlgeschlagen. Archive liegen lokal bereit.")
        sys.exit(1)


if __name__ == "__main__":
    main()
