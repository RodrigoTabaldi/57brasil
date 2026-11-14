"use client";

import { useEffect, useRef } from "react";
import { usePathname, useRouter } from "next/navigation";
import { ArrowLeft } from "lucide-react";

export function BackButton() {
  const pathname = usePathname();
  const router = useRouter();
  const currentPath = useRef<string | null>(null);
  const hasPreviousPage = useRef(false);

  useEffect(() => {
    if (currentPath.current && currentPath.current !== pathname) {
      hasPreviousPage.current = true;
    }
    currentPath.current = pathname;
  }, [pathname]);

  if (!pathname || pathname === "/") return null;

  function goBack() {
    const referrer = document.referrer;
    const cameFromThisSite = referrer && referrer !== window.location.href && new URL(referrer).origin === window.location.origin;

    if (hasPreviousPage.current || cameFromThisSite) {
      router.back();
    } else {
      router.push("/");
    }
  }

  return (
    <div className="shell back-navigation">
      <button className="back-button" type="button" onClick={goBack}>
        <ArrowLeft size={18} aria-hidden="true" /> Voltar à página anterior
      </button>
    </div>
  );
}
