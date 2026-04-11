"""
distribute.py — Post-Build Distribution Script fuer RemakeSoF.

Zippt einen fertigen Unity-Build, generiert ein SHA256-Manifest
und laedt beides an den AuthServer (FastAPI) hoch.

Verwendung:
    python Tools/distribute.py --build-dir Builds/Client/Windows10 --version 1.2.0
    python Tools/distribute.py --build-dir Builds/Client/Windows10 --version 1.2.0 --api-url https://api.remakesof.com
    python Tools/distribute.py --build-dir Builds/Server/Linux64 --version 1.2.0 --build-type server
"""

import argparse
import hashlib
import json
import os
import sys
import zipfile
from pathlib import Path

try:
    import requests
except ImportError:
    requests = None

DEFAULT_API_URL = "http://localhost:8000"
OUTPUT_DIR = "Builds/_dist"


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


def generate_manifest(build_dir: str, version: str, build_type: str) -> dict:
    """Erzeugt ein Manifest mit SHA256-Hashes und Dateigroessen fuer alle Dateien im Build."""
    files = []
    total_size = 0

    for root, _, filenames in os.walk(build_dir):
        for filename in filenames:
            filepath = os.path.join(root, filename)
            relative = os.path.relpath(filepath, build_dir).replace("\\", "/")
            file_hash = sha256_file(filepath)
            size = os.path.getsize(filepath)
            total_size += size
            files.append({
                "path": relative,
                "sha256": file_hash,
                "size": size,
            })

    print(f"  {len(files)} Dateien erfasst, Gesamtgroesse: {total_size / (1024 * 1024):.1f} MB")

    return {
        "version": version,
        "build_type": build_type,
        "file_count": len(files),
        "total_size": total_size,
        "files": files,
    }


def create_archive(build_dir: str, output_zip: str) -> str:
    """Erstellt ein ZIP-Archiv aus dem Build-Ordner und gibt den SHA256-Hash zurueck."""
    with zipfile.ZipFile(output_zip, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as zf:
        for root, _, filenames in os.walk(build_dir):
            for filename in filenames:
                filepath = os.path.join(root, filename)
                arcname = os.path.relpath(filepath, build_dir)
                zf.write(filepath, arcname)

    archive_size = os.path.getsize(output_zip)
    archive_hash = sha256_file(output_zip)
    print(f"  Archiv: {archive_size / (1024 * 1024):.1f} MB, SHA256: {archive_hash[:16]}...")
    return archive_hash


def upload_to_server(api_url: str, version: str, build_type: str,
                     zip_path: str, manifest: dict, token: str) -> bool:
    """Laedt ZIP + Manifest an den FastAPI AuthServer hoch."""
    if requests is None:
        print("FEHLER: 'requests' ist nicht installiert. Installiere mit: pip install requests")
        print(f"  Dateien liegen bereit in: {os.path.dirname(zip_path)}")
        return False

    url = f"{api_url}/api/game/upload"
    headers = {"Authorization": f"Bearer {token}"}

    print(f"  Uploading to {url} ...")

    with open(zip_path, "rb") as zip_file:
        files_payload = {
            "archive": (os.path.basename(zip_path), zip_file, "application/zip"),
            "manifest": ("manifest.json", json.dumps(manifest, indent=2), "application/json"),
        }
        data_payload = {
            "version": version,
            "build_type": build_type,
        }

        response = requests.post(
            url,
            headers=headers,
            files=files_payload,
            data=data_payload,
            timeout=600,
        )

    if response.status_code == 200:
        print(f"  Upload erfolgreich: {response.json()}")
        return True
    else:
        print(f"  Upload fehlgeschlagen: {response.status_code} — {response.text}")
        return False


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
        "--build-type", default="client", choices=["client", "server"],
        help="Build-Typ: client oder server (default: client)"
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
        "--output-dir", default=OUTPUT_DIR,
        help=f"Ausgabe-Ordner fuer ZIP + Manifest (default: {OUTPUT_DIR})"
    )

    args = parser.parse_args()

    build_dir = os.path.abspath(args.build_dir)
    if not os.path.isdir(build_dir):
        print(f"FEHLER: Build-Ordner existiert nicht: {build_dir}")
        sys.exit(1)

    output_dir = os.path.abspath(args.output_dir)
    os.makedirs(output_dir, exist_ok=True)

    zip_name = f"RemakeSoF-{args.build_type}-{args.version}.zip"
    zip_path = os.path.join(output_dir, zip_name)
    manifest_path = os.path.join(output_dir, f"manifest-{args.build_type}-{args.version}.json")

    # 1. Manifest generieren
    print(f"[1/3] Manifest generieren fuer {build_dir} ...")
    manifest = generate_manifest(build_dir, args.version, args.build_type)

    # 2. ZIP erstellen
    print(f"[2/3] Archiv erstellen: {zip_path} ...")
    archive_hash = create_archive(build_dir, zip_path)

    manifest["archive"] = {
        "filename": zip_name,
        "sha256": archive_hash,
        "size": os.path.getsize(zip_path),
    }

    # Manifest speichern
    with open(manifest_path, "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=2)
    print(f"  Manifest gespeichert: {manifest_path}")

    # 3. Upload
    if args.no_upload:
        print("[3/3] Upload uebersprungen (--no-upload).")
        print(f"\nFertig! Dateien liegen in: {output_dir}")
        return

    if not args.token:
        print("[3/3] Kein --token angegeben, Upload uebersprungen.")
        print(f"\nFertig! Dateien liegen in: {output_dir}")
        print(f"  Manueller Upload: POST {args.api_url}/api/game/upload")
        return

    print(f"[3/3] Upload an {args.api_url} ...")
    success = upload_to_server(
        args.api_url, args.version, args.build_type,
        zip_path, manifest, args.token
    )

    if success:
        print("\nDistribution abgeschlossen!")
    else:
        print("\nUpload fehlgeschlagen. ZIP + Manifest liegen lokal bereit.")
        sys.exit(1)


if __name__ == "__main__":
    main()
