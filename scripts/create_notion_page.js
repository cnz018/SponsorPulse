#!/usr/bin/env node
// Simple helper to create a Notion page from the local Markdown backlog.
// Usage:
//   Set env vars: NOTION_TOKEN, optional NOTION_PARENT_PAGE_ID, optional NOTION_PAGE_TITLE
//   From repo root: cd scripts && npm run create-notion-page

const fs = require('fs');
const path = require('path');
const { Client } = require('@notionhq/client');

const token = process.env.NOTION_TOKEN;
const parentId = process.env.NOTION_PARENT_PAGE_ID; // optional
const pageTitle = process.env.NOTION_PAGE_TITLE || 'Sponsorpulse';

if (!token) {
  console.error('ERROR: NOTION_TOKEN environment variable is required.');
  process.exit(1);
}

const notion = new Client({ auth: token });
const mdPath = path.resolve(__dirname, '../docs/NOTION_BACKLOG_SPONSORPULSE.md');

function mdToBlocks(md) {
  const lines = md.split(/\r?\n/);
  const blocks = [];
  for (const raw of lines) {
    const line = raw.trimRight();
    if (!line) { blocks.push({ object: 'block', type: 'paragraph', paragraph: { rich_text: [] } }); continue; }
    if (line.startsWith('# ')) {
      blocks.push({ object: 'block', type: 'heading_1', heading_1: { rich_text: [{ type: 'text', text: { content: line.replace(/^# /, '') } }] } });
    } else if (line.startsWith('## ')) {
      blocks.push({ object: 'block', type: 'heading_2', heading_2: { rich_text: [{ type: 'text', text: { content: line.replace(/^## /, '') } }] } });
    } else if (line.startsWith('- ')) {
      blocks.push({ object: 'block', type: 'bulleted_list_item', bulleted_list_item: { rich_text: [{ type: 'text', text: { content: line.replace(/^- /, '') } }] } });
    } else {
      blocks.push({ object: 'block', type: 'paragraph', paragraph: { rich_text: [{ type: 'text', text: { content: line } }] } });
    }
  }
  return blocks;
}

async function main() {
  if (!fs.existsSync(mdPath)) {
    console.error('ERROR: backlog Markdown not found at', mdPath);
    process.exit(1);
  }
  const md = fs.readFileSync(mdPath, 'utf8');
  const children = mdToBlocks(md);

  const parent = parentId ? { page_id: parentId } : { workspace: true };

  try {
    const response = await notion.pages.create({
      parent,
      properties: {
        title: {
          title: [ { type: 'text', text: { content: pageTitle } } ]
        }
      },
      children
    });

    console.log('Notion page created (id):', response.id);
    console.log('If creation failed due to parent, rerun with NOTION_PARENT_PAGE_ID set to a valid page id.');
  } catch (err) {
    console.error('Failed to create Notion page:', err.message || err);
    if (err && err.code === 'validation_error') {
      console.error('Validation error — likely parent type not allowed. Provide NOTION_PARENT_PAGE_ID.');
    }
    process.exit(1);
  }
}

main();
