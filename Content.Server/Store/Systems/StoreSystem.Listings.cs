using Content.Server.Store.Components;
using Content.Shared.Store;

namespace Content.Server.Store.Systems;

public sealed partial class StoreSystem : EntitySystem
{
    /// <summary>
    /// Refreshes all listings on a store.
    /// Do not use if you don't know what you're doing.
    /// </summary>
    /// <param name="component">The store to refresh</param>
    public void RefreshAllListings(StoreComponent component)
    {
        component.Listings = GetAllListings();
    }

    /// <summary>
    /// Gets all listings from a prototype.
    /// </summary>
    /// <returns>All the listings</returns>
    public HashSet<ListingData> GetAllListings()
    {
        var allListings = _proto.EnumeratePrototypes<ListingPrototype>();

        var allData = new HashSet<ListingData>();

        foreach (var listing in allListings)
            allData.Add(listing);

        return allData;
    }

    /// <summary>
    /// Adds a listing from an Id to a store
    /// </summary>
    /// <param name="component">The store to add the listing to</param>
    /// <param name="listingId">The id of the listing</param>
    /// <returns>Whetehr or not the listing was added successfully</returns>
    public bool TryAddListing(StoreComponent component, string listingId)
    {
        if (!_proto.TryIndex<ListingPrototype>(listingId, out var proto))
        {
            Logger.Error("Attempted to add invalid listing.");
            return false;
        }
        return TryAddListing(component, proto);
    }

    /// <summary>
    /// Adds a listing to a store
    /// </summary>
    /// <param name="component">The store to add the listing to</param>
    /// <param name="listing">The listing</param>
    /// <returns>Whether or not the listing was add successfully</returns>
    public bool TryAddListing(StoreComponent component, ListingData listing)
    {
        return component.Listings.Add(listing);
    }

    /// <summary>
    /// Gets the available listings for a store
    /// </summary>
    /// <param name="user">The person getting the listings.</param>
    /// <param name="component">The store the listings are coming from.</param>
    /// <returns>The available listings.</returns>
    public IEnumerable<ListingData> GetAvailableListings(EntityUid user, StoreComponent component)
    {
        return GetAvailableListings(user, component.Listings, component.Categories, component.Owner);
    }

    /// <summary>
    /// Gets the available listings for a user given an overall set of listings and categories to filter by.
    /// </summary>
    /// <param name="user">The person getting the listings.</param>
    /// <param name="listings">All of the listings that are available. If null, will just get all listings from the prototypes.</param>
    /// <param name="categories">What categories to filter by.</param>
    /// <param name="storeEntity">The physial entity of the store. Can be null.</param>
    /// <returns>The available listings.</returns>
    public IEnumerable<ListingData> GetAvailableListings(EntityUid user, HashSet<ListingData>? listings, HashSet<string> categories, EntityUid? storeEntity = null)
    {
        if (listings == null)
            listings = GetAllListings();

        foreach (var listing in listings)
        {
            if (!ListingHasCategory(listing, categories))
                continue;

            if (listing.Conditions != null)
            {
                var args = new ListingConditionArgs(user, storeEntity, listing, EntityManager);
                var conditionsMet = true;

                foreach (var condition in listing.Conditions)
                {
                    if (!condition.Condition(args))
                    {
                        conditionsMet = false;
                        break;
                    }
                }

                if (!conditionsMet)
                    continue;
            }

            yield return listing;
        }
    }

    /// <summary>
    /// Checks if a listing appears in a list of given categories
    /// </summary>
    /// <param name="listing">The listing itself.</param>
    /// <param name="categories">The categories to check through.</param>
    /// <returns>If the listing was present in one of the categories.</returns>
    public bool ListingHasCategory(ListingData listing, HashSet<string> categories)
    {
        foreach (var cat in categories)
        {
            if (listing.Categories.Contains(cat))
                return true;
        }
        return false;
    }
}
