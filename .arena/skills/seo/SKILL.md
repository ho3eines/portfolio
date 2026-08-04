---
name: seo
description: |
  Search Engine Optimization for web applications. TRIGGER on ANY of: SEO, search engine, meta tags, Open Graph, structured data, JSON-LD, schema.org, sitemap, robots.txt, canonical, page title optimization, meta description, social sharing preview, SERP, ranking. Use this skill for ALL SEO-related tasks — adding structured data, optimizing meta tags, generating sitemaps, improving page performance for SEO, or making a site crawlable. Even if the user just says "SEO" or mentions "better search ranking," invoke this skill.
---

# SEO Optimization Skill

## Overview

Optimize web applications for search engines and social sharing. This skill covers on-page SEO, structured data, technical SEO, and social meta tags.

## When to Apply

Apply ALL of the following checklist items to every page:

### 1. Meta Tags (Dynamic)

Every page MUST have dynamic, unique:

```html
<title>Page Title — Site Name</title>
<meta name="description" content="150-160 char unique description">
<meta name="keywords" content="relevant, keywords, here">
<meta name="robots" content="index, follow">
<link rel="canonical" href="https://domain.com/page-url">
<meta name="author" content="Author Name">
```

### 2. Open Graph (Social Sharing)

Every page MUST have:

```html
<meta property="og:type" content="website">
<meta property="og:title" content="Page Title">
<meta property="og:description" content="Description">
<meta property="og:image" content="https://domain.com/og-image.jpg">
<meta property="og:url" content="https://domain.com/page-url">
<meta property="og:site_name" content="Site Name">
<meta property="og:locale" content="en_US">
```

### 3. Twitter Cards

```html
<meta name="twitter:card" content="summary_large_image">
<meta name="twitter:title" content="Page Title">
<meta name="twitter:description" content="Description">
<meta name="twitter:image" content="https://domain.com/og-image.jpg">
```

### 4. Structured Data (JSON-LD)

For a portfolio/personal site, use `Person` schema:

```json
{
  "@context": "https://schema.org",
  "@type": "Person",
  "name": "Full Name",
  "jobTitle": "Job Title",
  "url": "https://domain.com",
  "sameAs": ["linkedin", "github", "twitter"],
  "image": "https://domain.com/photo.jpg",
  "description": "Bio text",
  "address": {
    "@type": "PostalAddress",
    "addressLocality": "City",
    "addressCountry": "Country"
  },
  "knowsAbout": ["Skill1", "Skill2", "Skill3"]
}
```

For projects, use `CreativeWork` schema.

### 5. Technical SEO

- `robots.txt` at root: Allow all, point to sitemap
- `sitemap.xml`: Dynamic XML with all public pages, lastmod, changefreq, priority
- HTTPS enforced
- Responsive design (mobile-first)
- Fast load time (< 3s)
- Semantic HTML5 tags (`<header>`, `<main>`, `<section>`, `<article>`, `<footer>`)
- Proper heading hierarchy (h1 → h2 → h3 — never skip levels)
- Alt text on all images
- Breadcrumbs where applicable

### 6. Performance SEO

- Minified CSS/JS
- Lazy loading for images: `loading="lazy"`
- Font preconnect/preload
- `meta name="theme-color"` for PWA
- `meta name="viewport"` with `viewport-fit=cover`

### 7. Content SEO

- Unique, descriptive page titles (50-60 chars)
- Meta descriptions (150-160 chars) with call-to-action
- Semantic heading structure
- Internal linking between pages
- Descriptive anchor text (never "click here")
- Image alt text describing the image content

## Dynamic SEO Pattern (Blazor/.NET)

For Blazor WASM, use `HeadOutlet` + `PageTitle` + custom `HeadContent`:

```csharp
// In each page:
<PageTitle>Dynamic Title from API</PageTitle>
<HeadContent>
    <meta name="description" content="@profile.MetaDescription" />
    <meta property="og:title" content="@profile.FullName" />
    <meta property="og:description" content="@profile.Bio" />
</HeadContent>
```

For API-driven SEO, add SEO fields to the Profile entity:
- `MetaDescription`
- `MetaKeywords`  
- `OgImageUrl`
- `GoogleAnalyticsId`

## Checklist

Before shipping, verify:
- [ ] Every page has unique `<title>` (50-60 chars)
- [ ] Every page has unique `<meta description>` (150-160 chars)
- [ ] Open Graph tags present on all pages
- [ ] Twitter Card tags present
- [ ] JSON-LD structured data on homepage
- [ ] robots.txt exists and points to sitemap
- [ ] sitemap.xml is valid and lists all public URLs
- [ ] Canonical URLs set
- [ ] Semantic HTML5 used throughout
- [ ] All images have alt text
- [ ] Heading hierarchy is correct (h1→h2→h3)
- [ ] Site is mobile-responsive
- [ ] PageSpeed score > 90
- [ ] HTTPS enabled
- [ ] No broken internal links
