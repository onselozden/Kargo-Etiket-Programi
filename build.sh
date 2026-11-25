#!/bin/bash
# Linux/Mono ile derleme: ./build.sh
cd "$(dirname "$0")"; mkdir -p bin
R=""; for f in Resources/*.png; do n=$(basename $f .png); R="$R -resource:$f,res.$n.png"; done
mcs -target:winexe -out:bin/Kargo-Etiket-Programi.exe -win32icon:app.ico -sdk:4.5 -optimize+ -codepage:utf8 -r:System.Windows.Forms.dll -r:System.Drawing.dll -r:System.Xml.dll $R *.cs
