#!/bin/bash

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Move into the sibling 'frontend' directory
cd "$SCRIPT_DIR/frontend" || {
  echo "Failed to find frontend directory"
  exit 1
}

# Run Angular build
ng build --configuration production

SOURCE_DIR="$SCRIPT_DIR/frontend/dist/frontend/browser"
DEST_DIR="$SCRIPT_DIR/src/wwwroot"

if [ ! -d "$SOURCE_DIR" ]; then
  echo "Source directory not found: $SOURCE_DIR"
  exit 1
fi

cp -r "$SOURCE_DIR"/* "$DEST_DIR"