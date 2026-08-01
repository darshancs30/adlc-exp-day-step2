#!/bin/sh
set -eu

INDEX_HTML='/usr/share/nginx/html/index.html'

API_URL="${VITE_API_URL:-}"

if [ -f "$INDEX_HTML" ]; then
  # Escape characters that break sed replacements.
  ESCAPED_API_URL=$(printf '%s' "$API_URL" | sed -e 's/[\/&]/\\&/g')
  sed -i "s/__VITE_API_URL__/${ESCAPED_API_URL}/g" "$INDEX_HTML"
fi

exec nginx -g 'daemon off;'
