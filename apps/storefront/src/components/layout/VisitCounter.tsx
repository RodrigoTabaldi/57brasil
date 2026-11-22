"use client";
import { usePathname } from "next/navigation";
import { useEffect } from "react";

export function VisitCounter() {
  const pathname = usePathname();
  useEffect(() => {
    if (pathname?.startsWith("/admin")) return;
    void fetch("/api/v1/visits", { method: "POST", keepalive: true });
  }, [pathname]);
  return null;
}
