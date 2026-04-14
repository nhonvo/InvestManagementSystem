import type {ReactNode} from 'react';
import clsx from 'clsx';
import Heading from '@theme/Heading';
import styles from './styles.module.css';

type FeatureItem = {
  title: string;
  Svg: React.ComponentType<React.ComponentProps<'svg'>>;
  description: ReactNode;
};

const FeatureList: FeatureItem[] = [
  {
    title: '.NET Clean Architecture',
    Svg: require('@site/static/img/undraw_docusaurus_mountain.svg').default,
    description: (
      <>
        A rock-solid backend built on <strong>.NET 10</strong>, adhering strictly 
        to Domain-Driven Design and Clean Architecture principles to separate 
        business logic from infrastructure.
      </>
    ),
  },
  {
    title: 'Real-Time Alert Engine',
    Svg: require('@site/static/img/undraw_docusaurus_tree.svg').default,
    description: (
      <>
        Powerful background workers using <strong>Hangfire</strong> and <strong>Amazon SQS</strong> 
        to synchronize Finnhub stock quotes and dispatch real-time Telegram notifications.
      </>
    ),
  },
  {
    title: 'Next.js 15 App Router',
    Svg: require('@site/static/img/undraw_docusaurus_react.svg').default,
    description: (
      <>
        A premium frontend application powered by <strong>React Server Components</strong>, 
        giving users lightning-fast interactions and seamless data fetching.
      </>
    ),
  },
];

function Feature({title, Svg, description}: FeatureItem) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center">
        <Svg className={styles.featureSvg} role="img" />
      </div>
      <div className="text--center padding-horiz--md">
        <Heading as="h3">{title}</Heading>
        <p>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures(): ReactNode {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
