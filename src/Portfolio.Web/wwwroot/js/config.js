// portfolio.config.js - Site-wide configuration
const PORTFOLIO_CONFIG = {
  profile: {
    name: "Mahbod Pour",
    title: "Frontend Developer & UI/UX Enthusiast",
    tagline: ["Crafting Digital", "Experiences", "That Inspire"],
    bio: "I build beautiful, performant, and accessible web experiences. With a deep passion for clean design and thoughtful interactions, I transform complex ideas into intuitive digital products that users love.",
    email: "mahbod@example.com",
    location: "Frankfurt am Main, Germany",
    social: {
      linkedin: "#",
      github: "#",
      twitter: "#"
    }
  },
  theme: {
    bg: "#0a0a0a",
    surface: "#141414",
    card: "#1a1a1a",
    cardAlt: "#202020",
    text: "#ffffff",
    textSecondary: "#9a9a9a",
    accent: "#d4a04c",
    border: "rgba(255,255,255,0.06)"
  },
  projects: [
    {
      id: 1,
      title: "Fintech Dashboard",
      category: "Web Application",
      desc: "Real-time financial analytics with interactive charts and portfolio tracking.",
      tech: ["Blazor WASM", "ASP.NET Core", "SQL Server", "Chart.js"]
    },
    {
      id: 2,
      title: "SaaS Platform",
      category: "SaaS",
      desc: "Multi-tenant platform with subscription billing and team management.",
      tech: ["React", ".NET Core", "PostgreSQL", "Stripe"]
    },
    {
      id: 3,
      title: "Brand Identity",
      category: "Design",
      desc: "Complete brand system including logo, colors, typography, and components.",
      tech: ["Figma", "CSS", "Storybook", "Design Tokens"]
    }
  ],
  skills: [
    { name: "HTML5 / CSS3", pct: 95, cat: "Frontend" },
    { name: "JavaScript", pct: 90, cat: "Frontend" },
    { name: "C# / .NET", pct: 92, cat: "Backend" },
    { name: "ASP.NET Core", pct: 90, cat: "Backend" },
    { name: "Blazor WASM", pct: 88, cat: "Frontend" },
    { name: "SQL Server", pct: 85, cat: "Database" },
    { name: "Tailwind CSS", pct: 90, cat: "Frontend" },
    { name: "React", pct: 85, cat: "Frontend" },
    { name: "Entity Framework", pct: 88, cat: "Backend" },
    { name: "TypeScript", pct: 82, cat: "Frontend" },
    { name: "Git / DevOps", pct: 92, cat: "DevOps" },
    { name: "Figma", pct: 80, cat: "Design" }
  ],
  experiences: [
    {
      company: "TechCorp GmbH",
      role: "Senior Frontend Developer",
      period: "2023 — Present",
      desc: "Leading frontend architecture. Reduced bundle size by 40%. Mentoring junior developers."
    },
    {
      company: "DigitalWave Agency",
      role: "Full-Stack Developer",
      period: "2021 — 2023",
      desc: "Delivered 15+ client projects. Introduced CI/CD pipelines reducing deployment time by 60%."
    },
    {
      company: "StartupHub",
      role: "Junior Developer",
      period: "2019 — 2020",
      desc: "Built responsive web apps with ASP.NET Core & React in an agile team of 8."
    }
  ],
  testimonials: [
    {
      name: "Alex Schmidt",
      role: "CTO, TechCorp GmbH",
      text: "Mahbod is one of the most talented developers I have worked with. His attention to detail and ability to translate complex requirements into elegant solutions is exceptional."
    },
    {
      name: "Sarah Müller",
      role: "Product Manager, DigitalWave",
      text: "Working with Mahbod was a game-changer. He consistently delivered pixel-perfect implementations ahead of schedule."
    },
    {
      name: "Dr. Reza Karimi",
      role: "Founder, StartupHub",
      text: "Mahbod brings a rare combination of technical excellence and creative vision. He doesn't just write code — he crafts experiences."
    }
  ]
};
