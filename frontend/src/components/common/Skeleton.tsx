import type { CSSProperties } from 'react';
import './skeleton.css';

interface SkeletonBlockProps {
  className?: string;
  width?: string;
  height?: string;
}

export function SkeletonBlock({ className = '', width, height }: SkeletonBlockProps) {
  const style: CSSProperties = {
    ...(width ? { width } : {}),
    ...(height ? { height } : {}),
  };

  return <span className={`skeleton-block ${className}`.trim()} style={style} aria-hidden="true" />;
}

interface SkeletonTableProps {
  columns: number;
  rows?: number;
}

export function SkeletonTable({ columns, rows = 5 }: SkeletonTableProps) {
  return (
    <div className="skeleton-table" role="status" aria-label="Đang tải dữ liệu">
      {Array.from({ length: rows }, (_, row) => (
        <div
          className="skeleton-table__row"
          key={row}
          style={{ gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))` }}
        >
          {Array.from({ length: columns }, (_, column) => (
            <SkeletonBlock
              className="skeleton-table__cell"
              height="15px"
              key={column}
              width={column === 0 ? '72%' : `${48 + ((row + column) % 3) * 12}%`}
            />
          ))}
        </div>
      ))}
    </div>
  );
}
