/**
 * Creating a sidebar enables you to:
 - create an ordered group of docs
 - render a sidebar for each doc of that group
 - provide next/previous navigation

 The sidebars can be generated from the filesystem, or explicitly defined here.

 Create as many sidebars as you want.
 */

// @ts-check

/** @type {import('@docusaurus/plugin-content-docs').SidebarsConfig} */
const sidebars = {
  // By default, Docusaurus generates a sidebar from the docs folder structure
  tutorialSidebar: [
    {
      type: 'doc',
      id: 'intro',
      label: 'Introduction',
    },
    'getting-started',
    'Configuration',
    {
      type: 'category',
      label: 'AWS Configuration',
      items: [
        'aws-configuration/README',
        'aws-configuration/credentials',
        'aws-configuration/regions',
        'aws-configuration/service-endpoints',
      ],
    },
    {
      type: 'category',
      label: 'Messaging Configuration',
      items: [
        'messaging-configuration/README',
        'messaging-configuration/logging',
        'messaging-configuration/metrics',
        'messaging-configuration/naming-conventions',
      ],
    },
    {
      type: 'category',
      label: 'Subscriptions',
      items: [
        {
          type: 'category',
          label: 'Configuration',
          items: [
            'subscriptions/configuration/README',
            'subscriptions/configuration/fortopic',
            'subscriptions/configuration/forqueue',
            'subscriptions/configuration/sqsreadconfiguration',
            'subscriptions/configuration/handler-registration-and-resolution',
          ],
        },
        {
          type: 'category',
          label: 'SubscriptionGroups',
          items: [
            'subscriptions/subscriptiongroups/README',
            'subscriptions/subscriptiongroups/configuration',
          ],
        },
        {
          type: 'category',
          label: 'Middleware',
          items: [
            'subscriptions/middleware/README',
            'subscriptions/middleware/custom-middleware',
          ],
        },
        'subscriptions/dead-letter-queues',
        'subscriptions/message-context',
      ],
    },
    {
      type: 'category',
      label: 'Publications',
      items: [
        'publishing/README',
        'publishing/configuration',
        'publishing/withtopic',
        'publishing/withqueue',
        'publishing/write-configuration',
        'publishing/batch-publishing',
      ],
    },
    {
      type: 'category',
      label: 'Advanced',
      items: [
        'advanced/README',
        'advanced/dynamic-topics',
        'advanced/compression',
        'advanced/encryption',
        'advanced/testing',
      ],
    },
    'how-justsaying-uses-sqs-sns',
    'interoperability',
    'aws-iam',
    'sample-application',
  ],
};

module.exports = sidebars;

