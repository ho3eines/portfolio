-- ============================================================
-- PortfolioDB — Seed Data
-- wwwroot/resources/02-seed-data.sql
-- ============================================================
USE [PortfolioDB];
GO

-- Roles
IF NOT EXISTS(SELECT 1 FROM Roles)
BEGIN
    INSERT INTO Roles(Name,Description) VALUES
    ('Admin','Full system access — can manage all content and users'),
    ('Editor','Can manage portfolio content but not users'),
    ('Viewer','Read-only access to admin panel');
END
GO

-- Admin user (Password: Admin@123 — hash is SHA256)
IF NOT EXISTS(SELECT 1 FROM Users WHERE Username='admin')
BEGIN
    INSERT INTO Users(Username,Email,PasswordHash,FullName,RoleId,Bio)
    VALUES('admin','admin@portfolio.com','8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918','Mahbod Pour',1,'Portfolio administrator');
END
GO

-- Profile
IF NOT EXISTS(SELECT 1 FROM Profile)
BEGIN
    INSERT INTO Profile(FullName,Title,TaglineLine1,TaglineLine2,TaglineLine3,Bio,Email,LinkedIn,GitHub,[Location],ShowreelUrl,MetaDescription,MetaKeywords,OgImageUrl,SiteUrl)
    VALUES(
        'Mahbod Pour',
        'Frontend Developer & UI/UX Enthusiast',
        'Crafting Digital','Experiences','That Inspire',
        'I am a passionate frontend developer and UI/UX enthusiast dedicated to building beautiful, performant, and accessible web experiences. With a deep love for clean design and thoughtful interactions, I transform complex ideas into intuitive digital products that users love.',
        'mahbod@example.com','https://linkedin.com/in/mahbodpour','https://github.com/mahbodpour','Frankfurt am Main, Germany',NULL,
        'Frontend Developer & UI/UX Enthusiast based in Frankfurt. Crafting digital experiences with Blazor, .NET, and modern web technologies.',
        'Mahbod Pour, frontend developer, Blazor, .NET, web developer Frankfurt, UI/UX, ASP.NET Core, C#',
        NULL,'https://mahbodpour.com'
    );
END
GO

-- Skills (with IsTag for the tag cloud)
IF NOT EXISTS(SELECT 1 FROM Skills)
BEGIN
    INSERT INTO Skills(Name,Category,Proficiency,IconClass,IsTag,SortOrder) VALUES
    ('HTML5 / CSS3',    'Frontend', 95, 'devicon-html5-plain',          0, 1),
    ('JavaScript',      'Frontend', 90, 'devicon-javascript-plain',     0, 2),
    ('C# / .NET',       'Backend',  92, 'devicon-csharp-plain',         0, 3),
    ('ASP.NET Core',    'Backend',  90, 'devicon-dotnetcore-plain',     0, 4),
    ('Blazor WASM',     'Frontend', 88, 'devicon-blazor-plain',         0, 5),
    ('SQL Server',      'Database', 85, 'devicon-microsoftsqlserver-plain',0,6),
    ('Tailwind CSS',    'Frontend', 90, 'devicon-tailwindcss-plain',    0, 7),
    ('Entity Framework','Backend',  88, 'devicon-dot-net-plain',        0, 8);
    -- Tags
    INSERT INTO Skills(Name,Category,Proficiency,IsTag,SortOrder) VALUES
    ('React',         'Frontend', 85, 1, 9),
    ('TypeScript',    'Frontend', 82, 1, 10),
    ('Bootstrap',     'Frontend', 88, 1, 11),
    ('Git',           'DevOps',   92, 1, 12),
    ('Figma',         'Design',   80, 1, 13),
    ('Docker',        'DevOps',   78, 1, 14),
    ('Azure',         'Cloud',    75, 1, 15),
    ('REST APIs',     'Backend',  90, 1, 16);
END
GO

-- Projects
IF NOT EXISTS(SELECT 1 FROM Projects)
BEGIN
    INSERT INTO Projects(Title,Slug,ShortDesc,[Description],Category,Technologies,MockupClass,CardRotation,SortOrder,IsFeatured)
    VALUES(
        'Fintech Dashboard','fintech-dashboard',
        'Real-time financial analytics with interactive charts and portfolio tracking.',
        'Designed and developed a comprehensive financial analytics platform enabling users to track investments, analyze market trends, and manage portfolios in real-time. Features include interactive charts, customizable dashboards, and secure multi-factor authentication.',
        'Web Application',
        '["Blazor WASM","ASP.NET Core","SQL Server","Chart.js","SignalR"]',
        'mockup-dark','-6deg',1,1
    );
    INSERT INTO Projects(Title,Slug,ShortDesc,[Description],Category,Technologies,MockupClass,CardRotation,SortOrder,IsFeatured)
    VALUES(
        'SaaS Platform','saas-platform',
        'Multi-tenant SaaS platform with subscription billing and team management.',
        'Architected a multi-tenant SaaS platform serving over 10,000 users. Implemented subscription billing, team management, role-based access control, and comprehensive analytics dashboards.',
        'SaaS',
        '["React",".NET Core","PostgreSQL","Stripe","Redis"]',
        'mockup-light','3deg',2,1
    );
    INSERT INTO Projects(Title,Slug,ShortDesc,[Description],Category,Technologies,MockupClass,CardRotation,SortOrder,IsFeatured)
    VALUES(
        'Brand Identity System','brand-identity',
        'Complete brand identity and design system for a tech startup.',
        'Created a comprehensive brand identity system including logo design, color palette, typography scale, component library, and design tokens.',
        'Design',
        '["Figma","CSS","Storybook","Design Tokens"]',
        'mockup-brand','-2deg',3,1
    );
END
GO

-- Experiences
IF NOT EXISTS(SELECT 1 FROM Experiences)
BEGIN
    INSERT INTO Experiences(Company,[Role],[Description],StartDate,EndDate,[Location],SortOrder)
    VALUES('TechCorp GmbH','Senior Frontend Developer','Leading frontend architecture for flagship SaaS product. Reduced bundle size by 40% through code splitting. Mentoring junior developers.','2023-03-01',NULL,'Frankfurt, Germany',1);
    INSERT INTO Experiences(Company,[Role],[Description],StartDate,EndDate,[Location],SortOrder)
    VALUES('DigitalWave Agency','Full-Stack Developer','Delivered 15+ client projects. Introduced CI/CD pipelines reducing deployment time by 60%.','2021-01-01','2023-02-28','Berlin, Germany',2);
    INSERT INTO Experiences(Company,[Role],[Description],StartDate,EndDate,[Location],SortOrder)
    VALUES('StartupHub','Junior Developer','Built responsive web apps with ASP.NET Core & React in an agile team of 8.','2019-06-01','2020-12-31','Tehran, Iran',3);
END
GO

-- Testimonials
IF NOT EXISTS(SELECT 1 FROM Testimonials)
BEGIN
    INSERT INTO Testimonials(ClientName,ClientTitle,Content,Rating,SortOrder)
    VALUES('Alex Schmidt','CTO, TechCorp GmbH','Mahbod is one of the most talented developers I have worked with. His attention to detail and ability to translate complex requirements into elegant solutions is exceptional.',5,1);
    INSERT INTO Testimonials(ClientName,ClientTitle,Content,Rating,SortOrder)
    VALUES('Sarah Müller','Product Manager, DigitalWave','Working with Mahbod was a game-changer for our team. He consistently delivered pixel-perfect implementations ahead of schedule. His UI/UX intuition is remarkable.',5,2);
    INSERT INTO Testimonials(ClientName,ClientTitle,Content,Rating,SortOrder)
    VALUES('Dr. Reza Karimi','Founder, StartupHub','Mahbod brings a rare combination of technical excellence and creative vision. He doesn''t just write code — he crafts experiences that users love.',5,3);
END
GO

-- Site Settings
IF NOT EXISTS(SELECT 1 FROM SiteSettings)
BEGIN
    INSERT INTO SiteSettings([Key],[Value]) VALUES
    ('hero_title1','Crafting Digital'),
    ('hero_title2','Experiences'),
    ('hero_title3','That Inspire'),
    ('hero_subtitle','Frontend Developer & UI/UX Enthusiast — building beautiful, performant, and accessible web experiences.'),
    ('section_work_label','SELECTED WORK'),
    ('section_work_title','Turning Ideas Into Impact'),
    ('section_about_label','ABOUT ME'),
    ('section_about_title','The Mind Behind The Pixels'),
    ('section_skills_label','TOOLKIT'),
    ('section_skills_title','Technologies I Work With'),
    ('section_contact_title','Let''s Build Something Great'),
    ('site_accent_color','#d4a04c');
END
GO

PRINT '✓ Seed data inserted.';
GO
