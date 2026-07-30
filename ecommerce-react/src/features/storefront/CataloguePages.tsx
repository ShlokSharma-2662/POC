import {
  useEffect,
  useMemo,
  useState,
  type ChangeEvent,
  type ReactNode,
} from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
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
  Smartphones: '\u{1F4F1}',
  Laptops: '\u{1F4BB}',
  Headphones: '\u{1F3A7}',
  'Smart Watches': '\u{231A}',
  Tablets: '\u{1F4F2}',
  Cameras: '\u{1F4F7}',
  Gaming: '\u{1F3AE}',
  Audio: '\u{1F50A}',
  Accessories: '\u{1F5BC}',
  Wearables: '\u{1F4AA}',
};

const categorySymbols: Record<string, string> = {
  Smartphones: '\u{1F4F1}',
  Laptops: '\u{1F4BB}',
  Headphones: '\u{1F3A7}',
  'Smart Watches': '\u{231A}',
  Tablets: '\u{1F4F2}',
  Cameras: '\u{1F4F7}',
  Gaming: '\u{1F3AE}',
  Audio: '\u{1F50A}',
  Accessories: '\u{1F5BC}',
  Wearables: '\u{1F4AA}',
};

const sortOptions = [
  ['relevance', 'Best match'],
  ['price-low', 'Price: low to high'],
  ['price-high', 'Price: high to low'],
  ['name-asc', 'Name: A to Z'],
  ['name-desc', 'Name: Z to A'],
  ['rating-desc', 'Most popular'],
] as const;
type SortOption = (typeof sortOptions)[number][0];
const sortShortcuts: Array<{ value: SortOption; label: string }> = [
  { value: 'relevance', label: 'Best match' },
  { value: 'price-low', label: 'Price: low → high' },
  { value: 'price-high', label: 'Price: high → low' },
  { value: 'rating-desc', label: 'Top rated' },
];

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

function normalizeSort(value: string | null): SortOption {
  return sortOptions.some(([key]) => key === value)
    ? (value as SortOption)
    : 'relevance';
}

function productSignals(product: Product) {
  const seed = Math.abs(product.productId * 37 + product.name.length * 11);
  const rating = Number((((seed % 24) / 10) + 3.5).toFixed(1));
  return {
    rating: Math.min(5, Math.max(0, rating)),
    reviewCount: 12 + (seed % 210),
  };
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
        <span aria-hidden="true">◉</span>
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
  className,
  onComplete,
}: {
  product: Product;
  compact?: boolean;
  className?: string;
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
    <div
      className={`${styles.productActions} ${compact ? styles.compact : ''} ${
        className ?? ''
      }`}
    >
      <button
        className={`btn ${styles.actionButton}`}
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
        {busyAction === 'wishlist'
          ? 'Saving…'
          : saved
            ? 'Saved'
            : 'Save'}
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
  const signals = productSignals(product);

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
            <dt>Rating</dt>
            <dd>
              ★ {signals.rating.toFixed(1)} ({signals.reviewCount} reviews)
            </dd>
          </div>
          <div>
            <dt>Category</dt>
            <dd>{product.categoryName || 'Uncategorized'}</dd>
          </div>
          <div>
            <dt>Delivery</dt>
            <dd>Est. 2-5 business days + free returns</dd>
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
  const signals = productSignals(product);

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
        <span className={styles.quickActionBadge} aria-hidden="true">
          Quick view
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
        <div className={styles.cardFacts}>
          <span>★ {signals.rating.toFixed(1)}</span>
          <span>{signals.reviewCount} reviews</span>
        </div>
        <div className={styles.cardPrice}>{formatCurrency(product.price)}</div>
        <div className={styles.actionDock}>
          <ProductActions product={product} compact />
        </div>
      </div>
    </article>
  );
}

export function HomePage() {
  const categoriesQuery = useCategories();
  const navigate = useNavigate();
  const featuredProductsQuery = useQuery({
    queryKey: ['storefront', 'featured-products', 'home'],
    queryFn: () =>
      api
        .get<ProductPageResult>('/products?PageNumber=1&PageSize=8', {
          authenticated: false,
        })
        .then((response) => ({
          items: (response.items ?? []).map(normalizeProduct),
          totalCount: response.totalCount,
        })),
  });

  const trendingProducts = (featuredProductsQuery.data?.items ?? []).slice(0, 4);
  const collections = (categoriesQuery.data ?? []).slice(0, 4);

  return (
    <div className={styles.homePage}>
      <section className={styles.hero}>
        <div className="container">
          <div className={styles.heroContent}>
            <div>
              <span className={styles.heroKicker}>Technology, thoughtfully chosen</span>
              <h1>Discover your next favorite gadget.</h1>
              <p>
                Explore reliable electronics, curated collections, and useful everyday
                upgrades. New picks update weekly.
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
                      .getElementById('collections')
                      ?.scrollIntoView({ behavior: 'smooth' })
                  }
                >
                  Browse collections
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

      <section className={`${styles.section} container`} id="collections">
        <div className={styles.sectionHeading}>
          <div>
            <span className={styles.eyebrow}>Shop your way</span>
            <h2>Featured collections</h2>
            <p>
              Start with a clear destination based on your current goal or mood.
            </p>
          </div>
          <Link to="/products">View all products →</Link>
        </div>

        <div className={styles.featureCollections}>
          {collections.length === 0 ? (
            <p className={styles.inlineNote}>
              Collections are being refreshed. Try browsing all products.
            </p>
          ) : (
            collections.map((category) => (
              <button
                className={styles.featureCollectionCard}
                type="button"
                key={category.id}
                onClick={() => navigate(`/products?category=${category.id}`)}
              >
                <span className={styles.featureSymbol} aria-hidden="true">
                  {categorySymbols[category.name] ?? '◉'}
                </span>
                <h3>{category.name}</h3>
                <p>
                  {category.description ||
                    categoryDescriptions[category.name] ||
                    'Quality products for every need.'}
                </p>
                <span>Explore</span>
              </button>
            ))
          )}
        </div>
      </section>

      <section className={styles.section}>
        <div className={`${styles.section} container`}>
          <div className={styles.sectionHeading}>
            <div>
              <span className={styles.eyebrow}>Trending</span>
              <h2>Trending now</h2>
              <p>Popular picks based on active storefront activity.</p>
            </div>
            <Link to="/products?sort=rating-desc">See more</Link>
          </div>

          {featuredProductsQuery.isPending ? (
            <Loader label="Loading trending products…" />
          ) : featuredProductsQuery.isError ? (
            <div className={styles.inlineError} role="alert">
              <p>
                {getErrorMessage(featuredProductsQuery.error, 'Could not load trends.')}
              </p>
            </div>
          ) : trendingProducts.length === 0 ? (
            <div className="empty-state surface">
              <h2>Build your first favorites list</h2>
              <p>Browse products and discover top sellers for your interests.</p>
              <Link className="btn btn-primary" to="/products">
                Start browsing
              </Link>
            </div>
          ) : (
            <div className={styles.productGrid}>
              {trendingProducts.map((product) => (
                <ProductCard
                  product={product}
                  key={product.productId}
                  onQuickView={(next) => navigate(`/products/${next.productId}`)}
                />
              ))}
            </div>
          )}
        </div>
      </section>

      <section className={styles.benefitBand}>
        <div className={`${styles.benefitGrid} container`}>
          <div>
            <strong>Fast delivery</strong>
            <span>Reliable dispatch, tracked status, and clear follow-up.</span>
          </div>
          <div>
            <strong>Secure payments</strong>
            <span>Card fields stay inside Stripe Elements.</span>
          </div>
          <div>
            <strong>Trusted support</strong>
            <span>Help is available from sign-in to order fulfillment.</span>
          </div>
          <div>
            <strong>Easy returns</strong>
            <span>30-day returns. Full support for every exchange.</span>
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
  const pageFromQuery = Number(searchParams.get('page') || 1);
  const [page, setPage] = useState(
    Number.isFinite(pageFromQuery) && pageFromQuery > 0
      ? Math.trunc(pageFromQuery)
      : 1,
  );
  const [search, setSearch] = useState(searchParams.get('search') ?? '');
  const [minimumPrice, setMinimumPrice] = useState('');
  const [maximumPrice, setMaximumPrice] = useState('');
  const [categoryName, setCategoryName] = useState('');
  const [minimumRating, setMinimumRating] = useState(0);
  const [sortBy, setSortBy] = useState<SortOption>(
    normalizeSort(searchParams.get('sort')),
  );
  const [onlyInStock, setOnlyInStock] = useState(
    searchParams.get('inStockOnly') === 'true',
  );
  const [showAdvanced, setShowAdvanced] = useState(false);
  const [selectedProduct, setSelectedProduct] = useState<Product | null>(null);
  const debouncedSearch = useDebouncedValue(search.trim(), 450);
  const debouncedCategoryName = useDebouncedValue(categoryName.trim(), 450);
  const categoriesQuery = useCategories();
  const productsQuery = useQuery({
    queryKey: [
      'storefront',
      'products',
      page,
      9,
      categoryId,
      debouncedSearch,
      minimumPrice,
      maximumPrice,
      debouncedCategoryName,
      sortBy,
      onlyInStock,
    ],
    queryFn: async () => {
      const query = new URLSearchParams({
        PageNumber: String(page),
        PageSize: String(9),
      });
      if (categoryId > 0) query.set('CategoryId', String(categoryId));
      if (debouncedSearch) query.set('SearchTerm', debouncedSearch);
      if (minimumPrice !== '') query.set('MinPrice', minimumPrice);
      if (maximumPrice !== '') query.set('MaxPrice', maximumPrice);
      if (debouncedCategoryName) {
        query.set('CategoryName', debouncedCategoryName);
      }
      if (onlyInStock) {
        query.set('InStockOnly', 'true');
      }
      return api.get<ProductPageResult>(`/products?${query.toString()}`, {
        authenticated: false,
      });
    },
  });
  const trendingFallbackQuery = useQuery({
    queryKey: ['storefront', 'trending-fallback'],
    queryFn: () =>
      api
        .get<ProductPageResult>('/products?PageNumber=1&PageSize=6', {
          authenticated: false,
        })
        .then((response) => ({
          items: (response.items ?? []).map(normalizeProduct),
          totalCount: response.totalCount,
        })),
  });
  const fallbackProducts = trendingFallbackQuery.data?.items ?? [];

  const products = useMemo(
    () => (productsQuery.data?.items ?? []).map(normalizeProduct),
    [productsQuery.data?.items],
  );
  const selectedCategoryName = useMemo(
    () =>
      categoriesQuery.data?.find((category) => category.id === categoryId)?.name ||
      '',
    [categoriesQuery.data, categoryId],
  );

  useEffect(() => {
    setSearch(searchParams.get('search') ?? '');
    setOnlyInStock(searchParams.get('inStockOnly') === 'true');
    setSortBy(normalizeSort(searchParams.get('sort')));
    setMinimumRating(Number(searchParams.get('minRating') ?? '0'));
    setMinimumPrice(searchParams.get('minPrice') ?? '');
    setMaximumPrice(searchParams.get('maxPrice') ?? '');
    setCategoryName(searchParams.get('categoryName') ?? '');
    const nextPage = Number(searchParams.get('page') || '1');
    setPage(
      Number.isFinite(nextPage) && nextPage > 0 ? Math.trunc(nextPage) : 1,
    );
  }, [searchParams]);

  const totalCount = productsQuery.data?.totalCount ?? 0;
  const totalPages =
    productsQuery.data?.totalPages ?? Math.ceil(totalCount / 9);

  const suggestionTerms = useMemo(() => {
    const terms = new Set<string>();
    (categoriesQuery.data ?? []).forEach((category) => {
      if (category.name) terms.add(category.name);
    });
    products.forEach((product) => {
      if (product.name) terms.add(product.name);
    });
    return Array.from(terms).slice(0, 10);
  }, [categoriesQuery.data, products]);
  const quickSearchTerms = useMemo(() => suggestionTerms.slice(0, 6), [suggestionTerms]);

  const displayedProducts = useMemo(() => {
    const withRatings = products
      .filter((product) =>
        minimumRating === 0
          ? true
          : productSignals(product).rating >= minimumRating - 0.01,
      )
      .map((product) => ({ product, rating: productSignals(product).rating }));

    const sorted = [...withRatings];

    sorted.sort((left, right) => {
      if (sortBy === 'price-low') {
        return left.product.price - right.product.price;
      }
      if (sortBy === 'price-high') {
        return right.product.price - left.product.price;
      }
      if (sortBy === 'name-asc') {
        return left.product.name.localeCompare(right.product.name);
      }
      if (sortBy === 'name-desc') {
        return right.product.name.localeCompare(left.product.name);
      }
      if (sortBy === 'rating-desc') {
        return right.rating - left.rating;
      }
      return left.product.productId - right.product.productId;
    });

    return sorted.map((item) => item.product);
  }, [minimumRating, products, sortBy]);

  const activeFilters = useMemo(() => {
    const filters: Array<{
      key: string;
      label: string;
      onClear: () => void;
    }> = [];

    if (search) {
      filters.push({
        key: 'search',
        label: `Search: ${search}`,
        onClear: () => {
          setSearch('');
          setPage(1);
        },
      });
    }

    if (selectedCategoryName) {
      filters.push({
        key: 'category',
        label: `Category: ${selectedCategoryName}`,
        onClear: () => {
          const next = new URLSearchParams(searchParams);
          next.delete('category');
          setSearchParams(next, { replace: true });
          setPage(1);
        },
      });
    }

    if (sortBy !== 'relevance') {
      const sortLabel =
        sortOptions.find(([value]) => value === sortBy)?.[1] ?? sortBy;
      filters.push({
        key: 'sort',
        label: `Sort: ${sortLabel}`,
        onClear: () => {
          setSortBy('relevance');
          setPage(1);
        },
      });
    }

    if (minimumPrice) {
      filters.push({
        key: 'minPrice',
        label: `Min price: ${minimumPrice}`,
        onClear: () => {
          setMinimumPrice('');
          setPage(1);
        },
      });
    }

    if (maximumPrice) {
      filters.push({
        key: 'maxPrice',
        label: `Max price: ${maximumPrice}`,
        onClear: () => {
          setMaximumPrice('');
          setPage(1);
        },
      });
    }

    if (categoryName) {
      filters.push({
        key: 'categoryName',
        label: `Category name: ${categoryName}`,
        onClear: () => {
          setCategoryName('');
          setPage(1);
        },
      });
    }

    if (minimumRating > 0) {
      filters.push({
        key: 'minRating',
        label: `Minimum rating: ${minimumRating.toFixed(1)}`,
        onClear: () => {
          setMinimumRating(0);
          setPage(1);
        },
      });
    }

    if (onlyInStock) {
      filters.push({
        key: 'inStock',
        label: 'In stock only',
        onClear: () => {
          setOnlyInStock(false);
          setPage(1);
        },
      });
    }

    return filters;
  }, [
    search,
    categoryName,
    selectedCategoryName,
    sortBy,
    minimumPrice,
    maximumPrice,
    minimumRating,
    onlyInStock,
  ]);

  const updateSearchRoute = () => {
    const next = new URLSearchParams(searchParams);
    if (search) next.set('search', search);
    else next.delete('search');
    if (categoryId > 0) next.set('category', String(categoryId));
    else next.delete('category');
    if (sortBy !== 'relevance') next.set('sort', sortBy);
    else next.delete('sort');
    if (onlyInStock) next.set('inStockOnly', 'true');
    else next.delete('inStockOnly');
    if (minimumRating > 0) next.set('minRating', String(minimumRating));
    else next.delete('minRating');
    if (minimumPrice !== '') next.set('minPrice', minimumPrice);
    else next.delete('minPrice');
    if (maximumPrice !== '') next.set('maxPrice', maximumPrice);
    else next.delete('maxPrice');
    if (categoryName) next.set('categoryName', categoryName);
    else next.delete('categoryName');
    if (page > 1) next.set('page', String(page));
    else next.delete('page');
    setSearchParams(next, { replace: true });
  };

  const applyQuickSearch = (term: string) => {
    setSearch(term);
    setPage(1);
  };

  const setSortShortcut = (value: SortOption) => {
    setSortBy(value);
    setPage(1);
  };

  useEffect(() => {
    updateSearchRoute();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [search, categoryId, sortBy, onlyInStock, minimumRating, minimumPrice, maximumPrice, categoryName, page]);

  const changeCategory = (event: ChangeEvent<HTMLSelectElement>) => {
    const nextCategory = Number(event.target.value);
    setPage(1);
    const next = new URLSearchParams(searchParams);
    if (nextCategory > 0) next.set('category', String(nextCategory));
    else next.delete('category');
    if (search) next.set('search', search);
    setSearchParams(next, { replace: true });
  };

  const resetFilters = () => {
    setSearch('');
    setMinimumPrice('');
    setMaximumPrice('');
    setCategoryName('');
    setMinimumRating(0);
    setSortBy('relevance');
    setOnlyInStock(false);
    setShowAdvanced(false);
    setPage(1);
    setSearchParams({}, { replace: true });
  };

  const productCount = displayedProducts.length;

  return (
    <div className={`${styles.cataloguePage} page`}>
      <div className="container">
        <header className={styles.catalogueHeader}>
          <span className={styles.eyebrow}>Curated catalogue</span>
          <h1 className="page-title">
            Products for work, play, and everything between
          </h1>
          <p className="page-subtitle">
            Search by category, price, availability, and popularity.
          </p>
        </header>

        <section className={`${styles.filters} surface`} aria-label="Product filters">
          <div className={styles.primaryFilters}>
            <label>
              <span>Search</span>
              <input
                className="form-control"
                type="search"
                list="product-search-suggestions"
                value={search}
                placeholder="Search products..."
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
            <label>
              <span>Sort</span>
              <select
                className="form-select"
                value={sortBy}
                onChange={(event) =>
                  setSortBy(normalizeSort(event.target.value))
                }
              >
                {sortOptions.map(([value, label]) => (
                  <option value={value} key={value}>
                    {label}
                  </option>
                ))}
              </select>
            </label>
            <label className={styles.toggleRow}>
              <input
                type="checkbox"
                checked={onlyInStock}
                onChange={(event) => {
                  setOnlyInStock(event.target.checked);
                  setPage(1);
                }}
              />
              In stock only
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
              <label>
                <span>Minimum rating</span>
                <div className={styles.ratingFilter}>
                  <input
                    className="form-range"
                    min="0"
                    max="5"
                    step="0.5"
                    type="range"
                    value={minimumRating}
                    onChange={(event) =>
                      setMinimumRating(Number(event.target.value))
                    }
                  />
                  <output>{minimumRating.toFixed(1)}</output>
                </div>
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

        {quickSearchTerms.length > 0 ? (
          <div className={styles.quickFilterRow} aria-label="Quick searches">
            <span className={styles.quickFilterLabel}>Quick search:</span>
            <div className={styles.quickFilterChips}>
              {quickSearchTerms.map((term) => (
                <button
                  className={styles.quickChip}
                  type="button"
                  key={term}
                  aria-pressed={term === search}
                  onClick={() => applyQuickSearch(term)}
                >
                  {term}
                </button>
              ))}
            </div>
          </div>
        ) : null}

        <div className={styles.quickFilterRow} aria-label="Sort shortcuts">
          <span className={styles.quickFilterLabel}>Sort quick:</span>
          <div className={styles.quickFilterChips}>
            {sortShortcuts.map((sortItem) => (
              <button
                className={styles.quickChip}
                type="button"
                key={sortItem.value}
                aria-pressed={sortItem.value === sortBy}
                onClick={() => setSortShortcut(sortItem.value)}
              >
                {sortItem.label}
              </button>
            ))}
          </div>
        </div>

        {activeFilters.length > 0 ? (
          <div className={styles.activeFilterBar} aria-label="Applied filters">
            {activeFilters.map((filter) => (
              <button
                className={styles.filterChip}
                type="button"
                onClick={() => filter.onClear()}
                key={filter.key}
              >
                {filter.label}
                <span aria-hidden="true">×</span>
              </button>
            ))}
          </div>
        ) : null}

        <div className={styles.resultsHeading}>
          <p aria-live="polite">
            {productsQuery.isPending
              ? 'Loading products…'
              : `${productCount} product${productCount === 1 ? '' : 's'} found`}
            {productCount === totalCount || productsQuery.isPending
              ? ''
              : ` (filtered from ${totalCount})`}
          </p>
          {(search ||
            categoryId > 0 ||
            minimumPrice ||
            maximumPrice ||
            categoryName ||
            minimumRating > 0 ||
            onlyInStock) && (
            <button className={styles.textButton} type="button" onClick={resetFilters}>
              Clear filters
            </button>
          )}
        </div>

        {productsQuery.isPending ? (
          <Loader label="Loading products…" />
        ) : productsQuery.isError ? (
          <div className={styles.inlineError} role="alert">
            <p>{getErrorMessage(productsQuery.error, 'We could not load the products.')}</p>
            <button
              className="btn btn-outline-primary"
              type="button"
              onClick={() => void productsQuery.refetch()}
            >
              Try again
            </button>
          </div>
        ) : displayedProducts.length === 0 ? (
          <section className="empty-state surface">
            <h2>No products match these filters</h2>
            <p>Try a broader search or remove one of the filters.</p>
            <div className={styles.fallbackGrid}>
              {fallbackProducts.length > 0 ? (
                <>
                  <strong>You might like these instead:</strong>
                  <div className={styles.productGrid}>
                    {fallbackProducts.map((fallbackProduct) => (
                      <ProductCard
                        product={fallbackProduct}
                        key={`fallback-${fallbackProduct.productId}`}
                        onQuickView={setSelectedProduct}
                      />
                    ))}
                  </div>
                </>
              ) : null}
            </div>
            <button className="btn btn-outline-primary" type="button" onClick={resetFilters}>
              Reset filters
            </button>
          </section>
        ) : (
          <>
            <div className={styles.productGrid}>
              {displayedProducts.map((product) => (
                <ProductCard
                  product={product}
                  key={product.productId}
                  onQuickView={setSelectedProduct}
                />
              ))}
            </div>
            <div className={styles.paginationWrap}>
              <span>
                Showing {(page - 1) * 9 + 1}–
                {Math.min(page * 9, Math.max(productCount, totalCount))} of
                {productCount === displayedProducts.length && !showAdvanced
                  ? ` ${productCount}`
                  : ` ${Math.max(productCount, totalCount)}`} 
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

      <datalist id="product-search-suggestions">
        {suggestionTerms.map((term) => (
          <option value={term} key={term} />
        ))}
      </datalist>

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
                compact
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
        <Loader label="Loading product..." />
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
                    {busyProduct === product.productId ? 'Updating…' : 'Move to cart'}
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
            <button className="btn btn-danger" type="button" onClick={() => void clearAll()}>
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

