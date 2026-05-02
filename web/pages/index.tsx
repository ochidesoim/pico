import { useEffect } from "react";
import { useRouter } from "next/router";

// Root route redirects to /login client-side.
// getServerSideProps cannot be used with static export (next export).
export default function Home() {
  const router = useRouter();

  useEffect(() => {
    router.replace("/login");
  }, [router]);

  // Render nothing while redirecting
  return null;
}
