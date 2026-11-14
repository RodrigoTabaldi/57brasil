"use client";

import { useEffect, useRef } from "react";
import Link from "next/link";
import { ChevronDown, Wrench } from "lucide-react";

type Department = { id: string; name: string; slug: string; parentId: string | null };

export function DepartmentMenu({ departments, demoGroups }: { departments: Department[]; demoGroups: string[] }) {
  const menuRef = useRef<HTMLDetailsElement>(null);

  useEffect(() => {
    function closeOnOutsidePointer(event: PointerEvent) {
      const menu = menuRef.current;
      if (menu?.open && !menu.contains(event.target as Node)) menu.open = false;
    }

    function closeOnEscape(event: KeyboardEvent) {
      const menu = menuRef.current;
      if (event.key === "Escape" && menu?.open) {
        menu.open = false;
        menu.querySelector("summary")?.focus();
      }
    }

    document.addEventListener("pointerdown", closeOnOutsidePointer);
    document.addEventListener("keydown", closeOnEscape);
    return () => {
      document.removeEventListener("pointerdown", closeOnOutsidePointer);
      document.removeEventListener("keydown", closeOnEscape);
    };
  }, []);

  const closeMenu = () => { if (menuRef.current) menuRef.current.open = false; };

  return <details className="department-menu" ref={menuRef}>
    <summary><Wrench size={17} aria-hidden="true" /> {demoGroups.length ? "Tipos de peças" : "Departamentos"} <ChevronDown size={16} aria-hidden="true" /></summary>
    <div className="department-panel">
      {departments.map(item => <Link href={`/produtos?categoria=${item.slug}`} key={item.slug} onClick={closeMenu}>{item.name}</Link>)}
      {demoGroups.map(group => <Link href={`/explorar?peca=${encodeURIComponent(group)}`} key={group} onClick={closeMenu}>{group}</Link>)}
      <Link href="/produtos" onClick={closeMenu}>Ver produtos cadastrados</Link>
    </div>
  </details>;
}
