import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const repoOwner = process.env.GITHUB_REPOSITORY_OWNER ?? 'nhonvo';
const repoName = process.env.GITHUB_REPOSITORY?.split('/')[1] ?? 'InventoryManagementSystem';

const config: Config = {
  title: 'InventoryAlert Wiki',
  tagline: 'Modern Inventory Management System Documentation',
  favicon: 'img/favicon.ico',

  url: `https://${repoOwner}.github.io`,
  baseUrl: `/${repoName}/`, // GitHub Pages project site: /<repoName>/

  organizationName: repoOwner,
  projectName: repoName,

  onBrokenLinks: 'ignore', // Changed to ignore during migration
  onBrokenMarkdownLinks: 'warn',

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          routeBasePath: '/',
          editUrl: `https://github.com/${repoOwner}/${repoName}/tree/main/InventoryAlert.Wiki/`,
        },
        blog: false, // Disable the blog plugin
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  markdown: {
    mermaid: true,
  },
  themes: ['@docusaurus/theme-mermaid'],
  themeConfig: {
    colorMode: {
      defaultMode: 'dark',
      respectPrefersColorScheme: true,
    },
    navbar: {
      title: 'InventoryAlert Wiki',
      items: [],
    },
    footer: {
      style: 'dark',
      links: [],
      copyright: `Copyright © ${new Date().getFullYear()} Truong Nhon - InventoryAlert. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
