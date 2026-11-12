
import './ui.css'

/**
 * HU-19 · Tabla accesible y reutilizable.
 *
 * columns: [{ key, label, render? }]
 * rows: lista de objetos
 */
export default function Table({
  columns,
  rows,
  rowKey = 'id',
  emptyMessage = 'No hay registros disponibles.',
  caption,
  className = '',
}) {
  return (
    <div className={`ui-table-wrapper ${className}`.trim()}>
      <table className="ui-table">
        {caption && <caption>{caption}</caption>}

        <thead>
          <tr>
            {columns.map((column) => (
              <th key={column.key} scope="col">
                {column.label}
              </th>
            ))}
          </tr>
        </thead>

        <tbody>
          {rows.length === 0 ? (
            <tr>
              <td colSpan={columns.length} className="ui-table__empty">
                {emptyMessage}
              </td>
            </tr>
          ) : (
            rows.map((row, index) => (
              <tr key={row[rowKey] ?? index}>
                {columns.map((column) => (
                  <td key={column.key}>
                    {column.render
                      ? column.render(row[column.key], row)
                      : row[column.key] ?? '—'}
                  </td>
                ))}
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  )
}
