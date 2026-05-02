import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  reactStrictMode: true,
  // Static HTML export — produces web/out/ when `npm run build` is run.
  // This folder is bundled into VeloForge.exe by PyInstaller.
  output: "export",
  trailingSlash: true,
  images: {
    unoptimized: true,
  },
};

export default nextConfig;
