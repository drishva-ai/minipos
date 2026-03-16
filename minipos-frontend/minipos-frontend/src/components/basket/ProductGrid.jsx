export default function ProductGrid({
  articles, categories, activeCategory, onSelectCategory, onAddItem
}) {
  const filtered = activeCategory === 'All'
    ? articles
    : articles.filter(a => a.category === activeCategory);

  return (
    <div className="product-grid-wrapper">
      {/* Category filter tabs */}
      <div className="category-bar">
        {categories.map(cat => (
          <button
            key={cat}
            className={`category-chip ${activeCategory === cat ? 'active' : ''}`}
            onClick={() => onSelectCategory(cat)}
          >
            {cat}
          </button>
        ))}
      </div>

      {/* Product tiles */}
      <div className="product-grid">
        {filtered.map(article => (
          <button
            key={article.id}
            className="product-tile"
            onClick={() => onAddItem(article.barcode)}
            title={`${article.name} — £${article.price.toFixed(2)}`}
          >
            <span className="product-tile__emoji">{article.emoji}</span>
            <span className="product-tile__name">{article.name}</span>
            <span className="product-tile__price">£{article.price.toFixed(2)}</span>
          </button>
        ))}
        {filtered.length === 0 && (
          <p className="product-grid__empty">No products in this category</p>
        )}
      </div>
    </div>
  );
}
