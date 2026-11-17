#!/bin/bash
# blb.sh fichier.blend script.py [lignes] : Blender en arrière-plan sur un .blend, sortie dans <script>.log
"/c/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b "$(cygpath -w "$1")" --python-exit-code 1 --python "$(cygpath -w "$2")" > "$2.log" 2>&1
echo "code $?"; grep -v "^Fra:\|^ *$\|Saved:\|Time:" "$2.log" | tail -${3:-15}
