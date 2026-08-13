import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/")({
  head: () => ({
    meta: [
      { title: "JobCard & Invoicing — C# .NET MAUI Starter" },
      {
        name: "description",
        content:
          "A .NET MAUI + ASP.NET Core starter for creating job cards and invoicing them against a central database you host yourself.",
      },
      { property: "og:title", content: "JobCard & Invoicing — C# .NET MAUI Starter" },
      {
        property: "og:description",
        content:
          "MAUI mobile app for iOS and Android, ASP.NET Core Web API and EF Core database, with job cards and invoice generation.",
      },
      { property: "og:type", content: "website" },
      { name: "twitter:card", content: "summary_large_image" },
    ],
  }),
  component: Index,
});

const projects = [
  {
    name: "JobCardApp.Shared",
    detail: "Customer, JobCard, JobCardLine, Invoice, InvoiceLine models plus InvoiceFactory.",
  },
  {
    name: "JobCardApp.Api",
    detail: "ASP.NET Core Web API + EF Core. SQLite by default, SQL Server with one config switch.",
  },
  {
    name: "JobCardApp.Mobile",
    detail: ".NET MAUI app targeting iOS and Android: job card list, editor with lines, invoices tab.",
  },
];

const steps = [
  { cmd: "dotnet workload install maui", note: "One-time setup" },
  {
    cmd: 'cd dotnet/src/JobCardApp.Api && dotnet run --urls "http://0.0.0.0:5080"',
    note: "Starts the API + database on your machine, Swagger at /swagger",
  },
  {
    cmd: "cd dotnet/src/JobCardApp.Mobile && dotnet build -t:Run -f net9.0-android",
    note: "Runs the app; set the URL in Services/ApiConfig.cs first",
  },
];

function Index() {
  return (
    <main className="min-h-screen bg-background px-6 py-16">
      <div className="mx-auto max-w-3xl">
        <p className="text-xs font-semibold uppercase tracking-[0.2em] text-muted-foreground">
          C# starter generated in /dotnet
        </p>
        <h1 className="mt-3 text-4xl font-bold tracking-tight text-foreground">
          Job cards &amp; invoicing
        </h1>
        <p className="mt-4 text-base leading-relaxed text-muted-foreground">
          A .NET MAUI app for iPhone and Android talking to an ASP.NET Core API and a central
          database you host yourself. Open{" "}
          <code className="rounded bg-muted px-1.5 py-0.5 text-sm text-foreground">
            dotnet/JobCardApp.sln
          </code>{" "}
          in Visual Studio and build from there.
        </p>

        <section className="mt-12">
          <h2 className="text-sm font-semibold uppercase tracking-wide text-foreground">
            Projects
          </h2>
          <ul className="mt-4 space-y-3">
            {projects.map((p) => (
              <li key={p.name} className="rounded-xl border border-border bg-card p-4">
                <p className="font-mono text-sm font-semibold text-card-foreground">{p.name}</p>
                <p className="mt-1 text-sm text-muted-foreground">{p.detail}</p>
              </li>
            ))}
          </ul>
        </section>

        <section className="mt-12">
          <h2 className="text-sm font-semibold uppercase tracking-wide text-foreground">
            Getting it running
          </h2>
          <ol className="mt-4 space-y-3">
            {steps.map((s, i) => (
              <li key={s.cmd} className="rounded-xl border border-border bg-card p-4">
                <div className="flex items-baseline gap-3">
                  <span className="font-mono text-xs text-muted-foreground">{i + 1}</span>
                  <code className="break-all text-sm text-card-foreground">{s.cmd}</code>
                </div>
                <p className="mt-2 pl-7 text-xs text-muted-foreground">{s.note}</p>
              </li>
            ))}
          </ol>
        </section>

        <p className="mt-12 text-sm text-muted-foreground">
          Full instructions, database options and next steps are in{" "}
          <code className="rounded bg-muted px-1.5 py-0.5 text-foreground">dotnet/README.md</code>.
        </p>
      </div>
    </main>
  );
}
