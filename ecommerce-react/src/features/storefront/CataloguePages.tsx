import {
  useEffect,
  useMemo,
  useState,
  type ChangeEvent,
  type ReactNode,
} from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Link,
  useNavigate,
  useParams,
  useSearchParams,
} from 'react-router-dom';
import { toast } from 'sonner';
import { Loader } from '../../components/ui/Loader';
import { Modal } from '../../components/ui/Modal';
import { Pagination } from '../../components/ui/Pagination';
import { api } from '../../lib/api-client';
import { useAuthStore } from '../../stores/auth-store';
import { useCartStore } from '../../stores/cart-store';
import { useWishlistStore } from '../../stores/wishlist-store';
import type { Product, WishlistItem } from '../../types/domain';
import styles from './Storefront.module.scss';
import {
  formatCurrency,
  getErrorMessage,
  normalizeProduct,
  type ProductPageResult,
  type StorefrontCategory,
  type StorefrontProduct,
} from './storefront-utils';

const categoryDescriptions: Record<string, string> = {
  Smartphones: 'Latest mobile devices with cutting-edge technology.',
  Laptops: 'Powerful computing for work, creativity, and entertainment.',
  Headphones: 'Premium audio with detail, comfort, and clarity.',
  'Smart Watches': 'Stay connected and keep track of your goals.',
  Tablets: 'Portable productivity and entertainment.',
  Cameras: 'Capture important moments in exceptional quality.',
  Gaming: 'Gear that makes every gaming session more immersive.',
  Audio: 'High-quality speakers and sound systems.',
  Accessories: 'Useful additions that make your devices even better.',
  Wearables: 'Smart technology designed to go everywhere with you.',
};

const categorySymbols: Record<string, string> = {
  Smartphones: '▯',
  Laptops: '⌨',
  Headphones: '◉',
  'Smart Watches': '◫',
  Tablets: '▤',
  Cameras: '◍',
  Gaming: '✦',
  Audio: '♫',
  Accessories: '⌁',
  Wearables: '◇',
};

function useCategories() {
  return useQuery({
    queryKey: ['storefront', 'categories'],
    queryFn: () =>
      api.get<StorefrontCategory[]>('/categories', {
        authenticated: false,
      }),
  });
}

function useDebouncedValue<T>(value: T, delay: number): T {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(value), delay);
    return () => window.clearTimeout(timer);
  }, [delay, value]);

  return debounced;
}

function ProductImage({
  src,
  alt,
  className,
}: {
  src?: string;
  alt: string;
  className?: string;
}) {
  const [failedSource, setFailedSource] = useState<string | undefined>();

  if (!src || failedSource === src) {
    return (
      <div
        className={`${styles.imagePlaceholder} ${className ?? ''}`}
        role="img"
        aria-label={`${alt} image unavailable`}
      >
        <span aria-hidden="true">◇</span>
      </div>
    );
  }

  return (
    <img
      className={className}
      src={src}
      alt={alt}
      loading="lazy"
      onError={() => setFailedSource(src)}
    />
  );
}

function ProductActions({
  product,
  compact = false,
  onComplete,
}: {
  product: Product;
  compact?: boolean;
  onComplete?: () => void;
}) {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const addToCart = useCartStore((state) => state.add);
  const addToWishlist = useWishlistStore((state) => state.add);
  const removeFromWishlist = useWishlistStore((state) => state.remove);
  const saved = useWishlistStore((state) =>
    isAuthenticated
      ? state.items.some((item) => item.productId === product.productId)
      : state.guestItems.some(
          (item) => item.product.productId === product.productId,
        ),
  );
  const [busyAction, setBusyAction] = useState<'cart' | 'wishlist' | null>(
    null,
  );
  const inStock = product.stock > 0;

  const add = async () => {
    setBusyAction('cart');
    try {
      await addToCart(product, 1);
      toast.success(`${product.name} was added to your cart.`);
      onComplete?.();
    } catch (error) {
      toast.error(getErrorMessage(error, 'Unable to update your cart.'));
    } finally {
      setBusyAction(null);
    }
  };

  const toggleSaved = async () => {
    setBusyAction('wishlist');
    try {
      if (saved) {
        await removeFromWishlist(product.productId);
        toast.success(`${product.name} was removed from your wishlist.`);
      } else {
        await addToWishlist(product);
        toast.success(`${product.name} was saved to your wishlist.`);
      }
    } catch (error) {
      toast.error(getErrorMessage(error, 'Unable to update your wishlist.'));
    } finally {
      setBusyAction(null);
    }
  };

  return (
    <div className={`${styles.productActions} ${compact ? styles.compact : ''}`}>
      <button
        className="btn btn-primary"
        type="button"
        disabled={!inStock || busyAction !== null}
        onClick={() => void add()}
      >
        {busyAction === 'cart'
          ? 'Adding…'
          : inStock
            ? 'Add to cart'
            : 'Out of stock'}
      </button>
      <button
        className={`btn ${saved ? 'btn-primary' : 'btn-outline-primary'}`}
        type="button"
        aria-pressed={saved}
        disabled={busyAction !== null}
        onClick={() => void toggleSaved()}
      >
        {busyAction === 'wishlist' ? 'Saving…' : saved ? 'Saved' : 'Save'}
      </button>
    </div>
  );
}

function ProductDetails({
  product,
  actions,
}: {
  product: Product;
  actions?: ReactNode;
}) {
  return (
    <div className={styles.productDetailGrid}>
      <ProductImage
        src={product.imageUrl}
        alt={product.name}
        className={styles.productDetailImage}
      />
      <div className={styles.productDetailContent}>
        <span className={styles.eyebrow}>
          {product.categoryName || 'Featured product'}
        </span>
        <h1>{product.name}</h1>
        <p className={styles.detailPrice}>{formatCurrency(product.price)}</p>
        <p>{product.description || 'No product description is available.'}</p>
        <dl className={styles.inlineFacts}>
          <div>
            <dt>Availability</dt>
            <dd>{product.stock > 0 ? `${product.stock} in stock` : 'Sold out'}</dd>
          </div>
          <div>
            <dt>Category</dt>
            <dd>{product.categoryName || 'Uncategorized'}</dd>
          </div>
        </dl>
        {actions}
      </div>
    </div>
  );
}

function ProductCard({
  product,
  onQuickView,
}: {
  product: Product;
  onQuickView: (product: Product) => void;
}) {
  const inStock = product.stock > 0;

  return (
    <article className={styles.productCard}>
      <button
        className={styles.productImageButton}
        type="button"
        onClick={() => onQuickView(product)}
        aria-label={`Quick view ${product.name}`}
      >
        <ProductImage
          src={product.imageUrl}
          alt={product.name}
          className={styles.productImage}
        />
        <span
          className={`${styles.stockBadge} ${
            inStock ? styles.inStock : styles.outOfStock
          }`}
        >
          {inStock ? `${product.stock} in stock` : 'Out of stock'}
        </span>
      </button>
      <div className={styles.productCardBody}>
        <span className={styles.eyebrow}>
          {product.categoryName || 'Uncategorized'}
        </span>
        <h2>
          <Link to={`/products/${product.productId}`}>{product.name}</Link>
        </h2>
        <p className={styles.cardDescription}>
          {product.description || 'Quality tech, selected for everyday use.'}
        </p>
        <div className={styles.cardPrice}>{formatCurrency(product.price)}</div>
        <ProductActions product={product} compact />
      </div>
    </article>
  );
}

export function HomePage() {
  const categoriesQuery = useCategories();
  const navigate = useNavigate();

  return (
    <div className={styles.homePage}>
      <section className={styles.hero}>
        <div className="container">
          <div className={styles.heroContent}>
            <div>
              <span className={styles.heroKicker}>Technology, thoughtfully chosen</span>
              <h1>Discover your next favorite gadget.</h1>
              <p>
                Explore reliable electronics, curated collections, and useful
                everyday upgrades.
              </p>
              <div className={styles.heroActions}>
                <Link className="btn btn-primary btn-lg" to="/products">
                  Shop now
                </Link>
                <button
                  className="btn btn-outline-light btn-lg"
                  type="button"
                  onClick={() =>
                    document
                      .getElementById('categories')
                      ?.scrollIntoView({ behavior: 'smooth' })
                  }
                >
                  Browse categories
                </button>
              </div>
            </div>
            <div className={styles.heroGraphic} aria-hidden="true">
              <div className={styles.orbit}>
                <span>Phone</span>
                <span>Laptop</span>
                <span>Audio</span>
                <strong>ShopSphere</strong>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className={`${styles.section} container`} id="categories">
        <div className={styles.sectionHeading}>
          <div>
            <span className={styles.eyebrow}>Shop your way</span>
            <h2>Browse by category</h2>
            <p>Jump directly to the products that fit what you need.</p>
          </div>
          <Link to="/products">View all products →</Link>
        </div>

        {categoriesQuery.isPending ? (
          <Loader label="Loading categories…" />
        ) : categoriesQuery.isError ? (
          <div className={styles.inlineError} role="alert">
            <p>
              {getErrorMessage(
                categoriesQuery.error,
                'We could not load the categories.',
              )}
            </p>
            <button
              className="btn btn-outline-primary"
              type="button"
              onClick={() => void categoriesQuery.refetch()}
            >
              Try again
            </button>
          </div>
        ) : (
          <div className={styles.categoryGrid}>
            {(categoriesQuery.data ?? []).map((category) => (
              <button
                className={styles.categoryCard}
                type="button"
                key={category.id}
                onClick={() => navigate(`/products?category=${category.id}`)}
              >
                <span className={styles.categorySymbol} aria-hidden="true">
                  {categorySymbols[category.name] ?? '◇'}
                </span>
                <span>
                  <strong>{category.name}</strong>
                  <small>
                    {category.description ||
                      categoryDescriptions[category.name] ||
                      'Quality products for every need.'}
                  </small>
                </span>
                <span aria-hidden="true">→</span>
              </button>
            ))}
          </div>
        )}
      </section>

      <section className={styles.benefitBand}>
        <div className={`${styles.benefitGrid} container`}>
          <div>
            <strong>Fast delivery</strong>
            <span>Reliable dispatch and clear order tracking.</span>
          </div>
          <div>
            <strong>Secure payments</strong>
            <span>Payment details stay inside Stripe Elements.</span>
          </div>
          <div>
            <strong>Helpful support</strong>
            <span>Assistance when you need it.</span>
          </div>
          <div>
            <strong>Easy returns</strong>
            <span>A straightforward 30-day return window.</span>
          </div>
        </div>
      </section>
    </div>
  );
}

export function ProductsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const categoryValue = Number(searchParams.get('category') || 0);
  const categoryId = Number.isFinite(categoryValue) ? categoryValue : 0;
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [minimumPrice, setMinimumPrice] = useState('');
  const [maximumPrice, setMaximumPrice] = useState('');
  const [categoryName, setCategoryName] = useState('');
  const [showAdvanced, setShowAdvanced] = useState(false);
  const [selectedProduct, setSelectedProduct] = useState<Product | null>(null);
  const debouncedSearch = useDebouncedValue(search.trim(), 450);
  const debouncedCategoryName = useDebouncedValue(categoryName.trim(), 450);
  const categoriesQuery = useCategories();
  const loadWishlist = useWishlistStore((state) => state.load);
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const pageSize = 9;

  useEffect(() => {
    void loadWishlist();
  }, [isAuthenticated, loadWishlist]);

  const productsQuery = useQuery({
    queryKey: [
      'storefront',
      'products',
      page,
      pageSize,
      categoryId,
      debouncedSearch,
      minimumPrice,
      maximumPrice,
      debouncedCategoryName,
    ],
    queryFn: async () => {
      const query = new URLSearchParams({
        PageNumber: String(page),
        PageSize: String(pageSize),
      });
      if (categoryId > 0) query.set('CategoryId', String(categoryId));
      if (debouncedSearch) query.set('SearchTerm', debouncedSearch);
      if (minimumPrice !== '') query.set('MinPrice', minimumPrice);
      if (maximumPrice !== '') query.set('MaxPrice', maximumPrice);
      if (debouncedCategoryName) {
        query.set('CategoryName', debouncedCategoryName);
      }
      return api.get<ProductPageResult>(`/products?${query.toString()}`, {
        authenticated: false,
      });
    },
  });

  const products = useMemo(
    () => (productsQuery.data?.items ?? []).map(normalizeProduct),
    [productsQuery.data?.items],
  );
  const totalCount = productsQuery.data?.totalCount ?? 0;
  const totalPages =
    productsQuery.data?.totalPages ?? Math.ceil(totalCount / pageSize);

  const changeCategory = (event: ChangeEvent<HTMLSelectElement>) => {
    const nextCategory = Number(event.target.value);
    setPage(1);
    const next = new URLSearchParams(searchParams);
    if (nextCategory > 0) next.set('category', String(nextCategory));
    else next.delete('category');
    setSearchParams(next, { replace: true });
  };

  const resetFilters = () => {
    setSearch('');
    setMinimumPrice('');
    setMaximumPrice('');
    setCategoryName('');
    setShowAdvanced(false);
    setPage(1);
    setSearchParams({}, { replace: true });
  };

  return (
    <div className={`${styles.cataloguePage} page`}>
      <div className="container">
        <header className={styles.catalogueHeader}>
          <span className={styles.eyebrow}>Curated catalogue</span>
          <h1 className="page-title">Products for work, play, and everything between</h1>
          <p className="page-subtitle">
            Search the complete catalogue or narrow it down by category and
            price.
          </p>
        </header>

        <section className={`${styles.filters} surface`} aria-label="Product filters">
          <div className={styles.primaryFilters}>
            <label>
              <span>Search</span>
              <input
                className="form-control"
                type="search"
                value={search}
                placeholder="Search products…"
                onChange={(event) => {
                  setSearch(event.target.value);
                  setPage(1);
                }}
              />
            </label>
            <label>
              <span>Category</span>
              <select
                className="form-select"
                value={categoryId}
                onChange={changeCategory}
              >
                <option value={0}>All categories</option>
                {(categoriesQuery.data ?? []).map((category) => (
                  <option value={category.id} key={category.id}>
                    {category.name}
                  </option>
                ))}
              </select>
            </label>
            <button
              className="btn btn-outline-primary"
              type="button"
              aria-expanded={showAdvanced}
              onClick={() => setShowAdvanced((value) => !value)}
            >
              {showAdvanced ? 'Hide filters' : 'More filters'}
            </button>
          </div>
          {showAdvanced ? (
            <div className={styles.advancedFilters}>
              <label>
                <span>Minimum price</span>
                <input
                  className="form-control"
                  min="0"
                  step="0.01"
                  type="number"
                  value={minimumPrice}
                  onChange={(event) => {
                    setMinimumPrice(event.target.value);
                    setPage(1);
                  }}
                />
              </label>
              <label>
                <span>Maximum price</span>
                <input
                  className="form-control"
                  min="0"
                  step="0.01"
                  type="number"
                  value={maximumPrice}
                  onChange={(event) => {
                    setMaximumPrice(event.target.value);
                    setPage(1);
                  }}
                />
              </label>
              <label>
                <span>Category name</span>
                <input
                  className="form-control"
                  type="search"
                  value={categoryName}
                  onChange={(event) => {
                    setCategoryName(event.target.value);
                    setPage(1);
                  }}
                />
              </label>
              <button
                className="btn btn-outline-secondary"
                type="button"
                onClick={resetFilters}
              >
                Reset all
              </button>
            </div>
          ) : null}
        </section>

        <div className={styles.resultsHeading}>
          <p aria-live="polite">
            {productsQuery.isPending
              ? 'Loading products…'
              : `${totalCount} product${totalCount === 1 ? '' : 's'} found`}
          </p>
          {(search ||
            categoryId > 0 ||
            minimumPrice ||
            maximumPrice ||
            categoryName) && (
            <button className={styles.textButton} type="button" onClick={resetFilters}>
              Clear filters
            </button>
          )}
        </div>

        {productsQuery.isPending ? (
          <Loader label="Loading products…" />
        ) : productsQuery.isError ? (
          <div className={styles.inlineError} role="alert">
            <p>
              {getErrorMessage(
                productsQuery.error,
                'We could not load the products.',
              )}
            </p>
            <button
              className="btn btn-outline-primary"
              type="button"
              onClick={() => void productsQuery.refetch()}
            >
              Try again
            </button>
          </div>
        ) : products.length === 0 ? (
          <div className="empty-state surface">
            <h2>No products found</h2>
            <p>Try a broader search or remove one of the filters.</p>
            <button className="btn btn-primary" type="button" onClick={resetFilters}>
              Reset filters
            </button>
          </div>
        ) : (
          <>
            <div className={styles.productGrid}>
              {products.map((product) => (
                <ProductCard
                  product={product}
                  key={product.productId}
                  onQuickView={setSelectedProduct}
                />
              ))}
            </div>
            <div className={styles.paginationWrap}>
              <span>
                Showing {(page - 1) * pageSize + 1}–
                {Math.min(page * pageSize, totalCount)} of {totalCount}
              </span>
              <Pagination
                page={page}
                totalPages={totalPages}
                onPageChange={(nextPage) => {
                  setPage(nextPage);
                  window.scrollTo({ top: 0, behavior: 'smooth' });
                }}
              />
            </div>
          </>
        )}
      </div>

      <Modal
        open={selectedProduct !== null}
        title={selectedProduct?.name ?? 'Product details'}
        size="lg"
        onClose={() => setSelectedProduct(null)}
      >
        {selectedProduct ? (
          <ProductDetails
            product={selectedProduct}
            actions={
              <ProductActions
                product={selectedProduct}
                onComplete={() => setSelectedProduct(null)}
              />
            }
          />
        ) : null}
      </Modal>
    </div>
  );
}

export function ProductDetailPage() {
  const params = useParams<{ productId?: string; id?: string }>();
  const rawId = params.productId ?? params.id ?? '';
  const productId = Number(rawId);
  const validId = Number.isInteger(productId) && productId > 0;
  const loadWishlist = useWishlistStore((state) => state.load);

  useEffect(() => {
    void loadWishlist();
  }, [loadWishlist]);

  const productQuery = useQuery({
    queryKey: ['storefront', 'product', productId],
    enabled: validId,
    queryFn: () =>
      api
        .get<StorefrontProduct>(`/products/${productId}`, {
          authenticated: false,
        })
        .then(normalizeProduct),
  });

  if (!validId) {
    return (
      <div className="container page">
        <div className="empty-state surface">
          <h1>Product not found</h1>
          <p>The product address is not valid.</p>
          <Link className="btn btn-primary" to="/products">
            Browse products
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="container page">
      <Link className={styles.backLink} to="/products">
        ← Back to products
      </Link>
      {productQuery.isPending ? (
        <Loader label="Loading product…" />
      ) : productQuery.isError ? (
        <div className={styles.inlineError} role="alert">
          <h1>Unable to load this product</h1>
          <p>{getErrorMessage(productQuery.error)}</p>
          <button
            className="btn btn-outline-primary"
            type="button"
            onClick={() => void productQuery.refetch()}
          >
            Try again
          </button>
        </div>
      ) : productQuery.data ? (
        <section className={`${styles.productDetailSurface} surface`}>
          <ProductDetails
            product={productQuery.data}
            actions={<ProductActions product={productQuery.data} />}
          />
        </section>
      ) : null}
    </div>
  );
}

function wishlistProduct(item: WishlistItem): Product {
  return {
    productId: item.productId,
    name: item.productName,
    description: item.productDescription,
    price: item.productPrice,
    stock: item.stock,
    imageUrl: item.productImageUrl,
    categoryName: item.categoryName,
    categoryId: 0,
  };
}

export function WishlistPage() {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const {
    items,
    guestItems,
    loading,
    load,
    remove,
    clear,
  } = useWishlistStore();
  const addToCart = useCartStore((state) => state.add);
  const [confirmingClear, setConfirmingClear] = useState(false);
  const [busyProduct, setBusyProduct] = useState<number | null>(null);

  useEffect(() => {
    void load();
  }, [isAuthenticated, load]);

  const products = isAuthenticated
    ? items.map((item) => ({
        product: wishlistProduct(item),
        addedAt: item.addedAt,
        inStock: item.isInStock,
      }))
    : guestItems.map((item) => ({
        product: item.product,
        addedAt: item.addedAt,
        inStock: item.product.stock > 0,
      }));
  const inStockCount = products.filter((item) => item.inStock).length;

  const removeItem = async (productId: number) => {
    setBusyProduct(productId);
    try {
      await remove(productId);
      toast.success('Product removed from your wishlist.');
    } catch (error) {
      toast.error(getErrorMessage(error, 'Unable to update your wishlist.'));
    } finally {
      setBusyProduct(null);
    }
  };

  const moveToCart = async (product: Product) => {
    setBusyProduct(product.productId);
    try {
      let completeProduct = product;
      if (isAuthenticated && product.categoryId === 0) {
        const response = await api.get<StorefrontProduct>(
          `/products/${product.productId}`,
          { authenticated: false },
        );
        completeProduct = normalizeProduct(response);
      }
      await addToCart(completeProduct, 1);
      await remove(product.productId);
      toast.success(`${product.name} was moved to your cart.`);
    } catch (error) {
      toast.error(getErrorMessage(error, 'Unable to move this product.'));
    } finally {
      setBusyProduct(null);
    }
  };

  const clearAll = async () => {
    try {
      await clear();
      toast.success('Your wishlist is now empty.');
      setConfirmingClear(false);
    } catch (error) {
      toast.error(getErrorMessage(error, 'Unable to clear your wishlist.'));
    }
  };

  return (
    <div className="container page">
      <header className="page-header">
        <div>
          <h1 className="page-title">Your wishlist</h1>
          <p className="page-subtitle">
            {products.length} saved · {inStockCount} currently in stock
          </p>
        </div>
        {products.length > 0 ? (
          <button
            className="btn btn-outline-danger"
            type="button"
            onClick={() => setConfirmingClear(true)}
          >
            Clear wishlist
          </button>
        ) : null}
      </header>

      {loading ? (
        <Loader label="Loading your wishlist…" />
      ) : products.length === 0 ? (
        <div className="empty-state surface">
          <h2>Save products for later</h2>
          <p>Your wishlist is empty. Find something worth coming back to.</p>
          <Link className="btn btn-primary" to="/products">
            Explore products
          </Link>
        </div>
      ) : (
        <div className={styles.wishlistGrid}>
          {products.map(({ product, addedAt, inStock }) => (
            <article className={`${styles.wishlistCard} surface`} key={product.productId}>
              <ProductImage
                src={product.imageUrl}
                alt={product.name}
                className={styles.wishlistImage}
              />
              <div className={styles.wishlistBody}>
                <span className={styles.eyebrow}>
                  {product.categoryName || 'Saved product'}
                </span>
                <h2>
                  <Link to={`/products/${product.productId}`}>{product.name}</Link>
                </h2>
                <p>{product.description}</p>
                <div className={styles.wishlistMeta}>
                  <strong>{formatCurrency(product.price)}</strong>
                  <span>{inStock ? 'In stock' : 'Out of stock'}</span>
                  <small>
                    Saved{' '}
                    {new Date(addedAt).toLocaleDateString('en-US', {
                      month: 'short',
                      day: 'numeric',
                      year: 'numeric',
                    })}
                  </small>
                </div>
                <div className={styles.wishlistActions}>
                  <button
                    className="btn btn-primary"
                    type="button"
                    disabled={!inStock || busyProduct === product.productId}
                    onClick={() => void moveToCart(product)}
                  >
                    {busyProduct === product.productId
                      ? 'Updating…'
                      : 'Move to cart'}
                  </button>
                  <button
                    className="btn btn-outline-danger"
                    type="button"
                    disabled={busyProduct === product.productId}
                    onClick={() => void removeItem(product.productId)}
                  >
                    Remove
                  </button>
                </div>
              </div>
            </article>
          ))}
        </div>
      )}

      <Modal
        open={confirmingClear}
        title="Clear your wishlist?"
        size="sm"
        onClose={() => setConfirmingClear(false)}
        footer={
          <>
            <button
              className="btn btn-outline-secondary"
              type="button"
              onClick={() => setConfirmingClear(false)}
            >
              Keep items
            </button>
            <button
              className="btn btn-danger"
              type="button"
              onClick={() => void clearAll()}
            >
              Clear wishlist
            </button>
          </>
        }
      >
        This removes every saved product from this wishlist.
      </Modal>
    </div>
  );
}
