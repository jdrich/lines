#!/bin/sh
ASPNETCORE_URLS="https://*:63002" nohup dotnet run >run.log 2>&1 &
