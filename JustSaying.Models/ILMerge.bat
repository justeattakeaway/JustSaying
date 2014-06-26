if not exist merged md merged

"..\packages\ilmerge.2.13.0307\ILMerge.exe" /out:merged\JustSaying.Models.dll /internalize /targetplatform:v4 "bin\Release\JustSaying.Models.dll"