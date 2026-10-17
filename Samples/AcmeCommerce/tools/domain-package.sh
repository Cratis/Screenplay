#!/bin/bash
# Assembles ONE domain of Acme Commerce as a standalone model - the unit Studio imports (one ZIP per event model):
#   `domain <X>` + Shared/ + Application/ (catalog only) + <X>/  ->  compile --warnaserror, run the specifications, zip.
# The combined application gives a foreign public event a consumer-local name `<Event>At<Consumer>` (two declarations of one
# name do not bind in one application). A standalone domain has no such clash, so by default the package gives every inbound
# contract its PUBLISHED name again (`ProductPublishedAtStorefront` -> `ProductPublished`); KEEP_LOCAL_NAMES=1 keeps the local names.
#
# usage:  tools/domain-package.sh Catalog|Storefront|Sales|Purchasing|Warehouse [output-dir]
#   SCREENPLAY="screenplay"   the compiler command (default: `screenplay` on PATH; or "dotnet run --project <repo>/Source/DotNET/Tool --")
set -e
DOMAIN=$1
[ -n "$DOMAIN" ] || { echo "usage: $0 <Domain> [output-dir]"; exit 2; }
HERE="$(cd "$(dirname "$0")" && pwd)"; SAMPLE="$(dirname "$HERE")"
OUT="${2:-${TMPDIR:-/tmp}/acme-commerce-packages}"; SCREENPLAY=${SCREENPLAY:-screenplay}
[ -d "$SAMPLE/$DOMAIN" ] || { echo "no such domain folder: $SAMPLE/$DOMAIN"; exit 2; }
P="$OUT/$DOMAIN"; rm -rf "$P" "$OUT/$DOMAIN.zip"; mkdir -p "$P"
# the root file carries the `domain` line and the application preamble (reaction batches atomic, command reads protected)
sed -E "s/^domain +[A-Za-z0-9]+\$/domain $DOMAIN/" "$SAMPLE/application.play" > "$P/domain.play"
cp -R "$SAMPLE/Shared" "$P/Shared"; cp -R "$SAMPLE/$DOMAIN" "$P/$DOMAIN"
# the shell state screens belong to the application, so they travel with the domain that has to stand alone: Catalog carries them
[ "$DOMAIN" = "Catalog" ] && cp -R "$SAMPLE/Application" "$P/Application"
if [ "$KEEP_LOCAL_NAMES" != "1" ]; then
  # inbound contracts declared as: event <Published>At<Consumer> from "<Origin>"
  grep -rhoE 'event +[A-Za-z0-9]+At[A-Za-z0-9]+ +from +"[A-Za-z]+"' "$P/$DOMAIN" 2>/dev/null | awk '{print $2}' | sort -u | while read -r LOCAL; do
    PUBLISHED=$(echo "$LOCAL" | sed -E "s/At${DOMAIN}\$//" | sed -E 's/At[A-Za-z0-9]+$//')
    find "$P/$DOMAIN" -name "*.play" -exec sed -i.bak -E "s/\\b${LOCAL}\\b/${PUBLISHED}/g" {} \; ; find "$P" -name "*.bak" -delete
    echo "published name: $LOCAL -> $PUBLISHED"
  done
fi
echo "== compile";  $SCREENPLAY --warnaserror "$P" | tail -1
echo "== specifications"; $SCREENPLAY test "$P" | tail -1
(cd "$P" && zip -qr "$OUT/$DOMAIN.zip" . -x '*.DS_Store') && echo "zip: $OUT/$DOMAIN.zip ($(unzip -l "$OUT/$DOMAIN.zip" | tail -1 | awk '{print $2}') files)"
