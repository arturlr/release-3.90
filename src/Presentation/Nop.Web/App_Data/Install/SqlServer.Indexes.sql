-- ---------------------------------------------------------------------------------------
-- nopCommerce custom (hand-tuned) index definitions, executed by
-- Nop.Data.Initializers.CreateTablesIfNotExist immediately after
-- DbContext.Database.GenerateCreateScript().
--
-- TASK 7.7: every CREATE INDEX below is now preceded by a guarded DROP INDEX. This is NOT
-- cosmetic - without it a fresh installation ABORTS.
--
-- WHY. EF Core's ForeignKeyIndexConvention creates an index on every foreign-key column and
-- names it IX_<Table>_<Column>. EF6's equivalent convention named it IX_<Column>. 3.90's
-- installer therefore never collided with the generated schema, and on EF Core 15 of the
-- names below now do:
--
--   IX_BlogComment_BlogPostId      IX_BlogPost_LanguageId       IX_Forums_Forum_ForumGroupId
--   IX_Forums_Post_CustomerId      IX_Forums_Post_TopicId       IX_Forums_Topic_ForumId
--   IX_NewsComment_NewsItemId      IX_News_LanguageId           IX_Order_CustomerId
--   IX_OrderItem_OrderId           IX_OrderNote_OrderId         IX_PollAnswer_PollId
--   IX_ProductReview_ProductId     IX_StateProvince_CountryId   IX_TierPrice_ProductId
--
-- Measured before this change: installation failed with
--   "Setup failed: The operation failed because an index or statistics with name
--    'IX_StateProvince_CountryId' already exists on table 'StateProvince'."
-- and, because the script aborts at the first failure, none of the 57 later indexes was
-- created either.
--
-- WHY DROP-AND-CREATE RATHER THAN "IF NOT EXISTS". The definitions here must WIN. They are
-- nopCommerce's deliberate physical design and several are strictly richer than the
-- convention index they collide with - IX_StateProvince_CountryId carries
-- INCLUDE ([DisplayOrder]), IX_Order_CustomerId is ordered, and so on. Skipping the CREATE
-- when a same-named index already existed would silently keep EF Core's narrower version and
-- lose those columns, which is exactly the kind of quiet degradation this migration is
-- supposed to surface rather than absorb.
--
-- The guard is deliberately written with sys.indexes/OBJECT_ID rather than
-- DROP INDEX IF EXISTS, which requires SQL Server 2016+; nopCommerce 3.90 supports 2008+.
--
-- It is also idempotent, which matters for the 2.x upgrade path: this script is re-run
-- against an existing database, where previously a second run would have failed on the very
-- first statement.
-- ---------------------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LocaleStringResource' AND object_id = OBJECT_ID(N'[LocaleStringResource]')) DROP INDEX [IX_LocaleStringResource] ON [LocaleStringResource]
GO
CREATE NONCLUSTERED INDEX [IX_LocaleStringResource] ON [LocaleStringResource] ([ResourceName] ASC,  [LanguageId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Product_PriceDatesEtc' AND object_id = OBJECT_ID(N'[Product]')) DROP INDEX [IX_Product_PriceDatesEtc] ON [Product]
GO
CREATE NONCLUSTERED INDEX [IX_Product_PriceDatesEtc] ON [Product]  ([Price] ASC, [AvailableStartDateTimeUtc] ASC, [AvailableEndDateTimeUtc] ASC, [Published] ASC, [Deleted] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Country_DisplayOrder' AND object_id = OBJECT_ID(N'[Country]')) DROP INDEX [IX_Country_DisplayOrder] ON [Country]
GO
CREATE NONCLUSTERED INDEX [IX_Country_DisplayOrder] ON [Country] ([DisplayOrder] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StateProvince_CountryId' AND object_id = OBJECT_ID(N'[StateProvince]')) DROP INDEX [IX_StateProvince_CountryId] ON [StateProvince]
GO
CREATE NONCLUSTERED INDEX [IX_StateProvince_CountryId] ON [StateProvince] ([CountryId]) INCLUDE ([DisplayOrder])
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Currency_DisplayOrder' AND object_id = OBJECT_ID(N'[Currency]')) DROP INDEX [IX_Currency_DisplayOrder] ON [Currency]
GO
CREATE NONCLUSTERED INDEX [IX_Currency_DisplayOrder] ON [Currency] ( [DisplayOrder] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Log_CreatedOnUtc' AND object_id = OBJECT_ID(N'[Log]')) DROP INDEX [IX_Log_CreatedOnUtc] ON [Log]
GO
CREATE NONCLUSTERED INDEX [IX_Log_CreatedOnUtc] ON [Log] ([CreatedOnUtc] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Customer_Email' AND object_id = OBJECT_ID(N'[Customer]')) DROP INDEX [IX_Customer_Email] ON [Customer]
GO
CREATE NONCLUSTERED INDEX [IX_Customer_Email] ON [Customer] ([Email] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Customer_Username' AND object_id = OBJECT_ID(N'[Customer]')) DROP INDEX [IX_Customer_Username] ON [Customer]
GO
CREATE NONCLUSTERED INDEX [IX_Customer_Username] ON [Customer] ([Username] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Customer_CustomerGuid' AND object_id = OBJECT_ID(N'[Customer]')) DROP INDEX [IX_Customer_CustomerGuid] ON [Customer]
GO
CREATE NONCLUSTERED INDEX [IX_Customer_CustomerGuid] ON [Customer] ([CustomerGuid] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Customer_SystemName' AND object_id = OBJECT_ID(N'[Customer]')) DROP INDEX [IX_Customer_SystemName] ON [Customer]
GO
CREATE NONCLUSTERED INDEX [IX_Customer_SystemName] ON [Customer] ([SystemName] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GenericAttribute_EntityId_and_KeyGroup' AND object_id = OBJECT_ID(N'[GenericAttribute]')) DROP INDEX [IX_GenericAttribute_EntityId_and_KeyGroup] ON [GenericAttribute]
GO
CREATE NONCLUSTERED INDEX [IX_GenericAttribute_EntityId_and_KeyGroup] ON [GenericAttribute] ([EntityId] ASC, [KeyGroup] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_QueuedEmail_CreatedOnUtc' AND object_id = OBJECT_ID(N'[QueuedEmail]')) DROP INDEX [IX_QueuedEmail_CreatedOnUtc] ON [QueuedEmail]
GO
CREATE NONCLUSTERED INDEX [IX_QueuedEmail_CreatedOnUtc] ON [QueuedEmail] ([CreatedOnUtc] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Order_CustomerId' AND object_id = OBJECT_ID(N'[Order]')) DROP INDEX [IX_Order_CustomerId] ON [Order]
GO
CREATE NONCLUSTERED INDEX [IX_Order_CustomerId] ON [Order] ([CustomerId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Language_DisplayOrder' AND object_id = OBJECT_ID(N'[Language]')) DROP INDEX [IX_Language_DisplayOrder] ON [Language]
GO
CREATE NONCLUSTERED INDEX [IX_Language_DisplayOrder] ON [Language] ([DisplayOrder] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BlogPost_LanguageId' AND object_id = OBJECT_ID(N'[BlogPost]')) DROP INDEX [IX_BlogPost_LanguageId] ON [BlogPost]
GO
CREATE NONCLUSTERED INDEX [IX_BlogPost_LanguageId] ON [BlogPost] ([LanguageId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BlogComment_BlogPostId' AND object_id = OBJECT_ID(N'[BlogComment]')) DROP INDEX [IX_BlogComment_BlogPostId] ON [BlogComment]
GO
CREATE NONCLUSTERED INDEX [IX_BlogComment_BlogPostId] ON [BlogComment] ([BlogPostId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_News_LanguageId' AND object_id = OBJECT_ID(N'[News]')) DROP INDEX [IX_News_LanguageId] ON [News]
GO
CREATE NONCLUSTERED INDEX [IX_News_LanguageId] ON [News] ([LanguageId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NewsComment_NewsItemId' AND object_id = OBJECT_ID(N'[NewsComment]')) DROP INDEX [IX_NewsComment_NewsItemId] ON [NewsComment]
GO
CREATE NONCLUSTERED INDEX [IX_NewsComment_NewsItemId] ON [NewsComment] ([NewsItemId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NewsletterSubscription_Email_StoreId' AND object_id = OBJECT_ID(N'[NewsLetterSubscription]')) DROP INDEX [IX_NewsletterSubscription_Email_StoreId] ON [NewsLetterSubscription]
GO
CREATE NONCLUSTERED INDEX [IX_NewsletterSubscription_Email_StoreId] ON [NewsLetterSubscription] ([Email] ASC, [StoreId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PollAnswer_PollId' AND object_id = OBJECT_ID(N'[PollAnswer]')) DROP INDEX [IX_PollAnswer_PollId] ON [PollAnswer]
GO
CREATE NONCLUSTERED INDEX [IX_PollAnswer_PollId] ON [PollAnswer] ([PollId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProductReview_ProductId' AND object_id = OBJECT_ID(N'[ProductReview]')) DROP INDEX [IX_ProductReview_ProductId] ON [ProductReview]
GO
CREATE NONCLUSTERED INDEX [IX_ProductReview_ProductId] ON [ProductReview] ([ProductId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderItem_OrderId' AND object_id = OBJECT_ID(N'[OrderItem]')) DROP INDEX [IX_OrderItem_OrderId] ON [OrderItem]
GO
CREATE NONCLUSTERED INDEX [IX_OrderItem_OrderId] ON [OrderItem] ([OrderId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderNote_OrderId' AND object_id = OBJECT_ID(N'[OrderNote]')) DROP INDEX [IX_OrderNote_OrderId] ON [OrderNote]
GO
CREATE NONCLUSTERED INDEX [IX_OrderNote_OrderId] ON [OrderNote] ([OrderId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TierPrice_ProductId' AND object_id = OBJECT_ID(N'[TierPrice]')) DROP INDEX [IX_TierPrice_ProductId] ON [TierPrice]
GO
CREATE NONCLUSTERED INDEX [IX_TierPrice_ProductId] ON [TierPrice] ([ProductId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ShoppingCartItem_ShoppingCartTypeId_CustomerId' AND object_id = OBJECT_ID(N'[ShoppingCartItem]')) DROP INDEX [IX_ShoppingCartItem_ShoppingCartTypeId_CustomerId] ON [ShoppingCartItem]
GO
CREATE NONCLUSTERED INDEX [IX_ShoppingCartItem_ShoppingCartTypeId_CustomerId] ON [ShoppingCartItem] ([ShoppingCartTypeId] ASC, [CustomerId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RelatedProduct_ProductId1' AND object_id = OBJECT_ID(N'[RelatedProduct]')) DROP INDEX [IX_RelatedProduct_ProductId1] ON [RelatedProduct]
GO
CREATE NONCLUSTERED INDEX [IX_RelatedProduct_ProductId1] ON [RelatedProduct] ([ProductId1] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProductAttributeValue_ProductAttributeMappingId_DisplayOrder' AND object_id = OBJECT_ID(N'[ProductAttributeValue]')) DROP INDEX [IX_ProductAttributeValue_ProductAttributeMappingId_DisplayOrder] ON [ProductAttributeValue]
GO
CREATE NONCLUSTERED INDEX [IX_ProductAttributeValue_ProductAttributeMappingId_DisplayOrder] ON [ProductAttributeValue] ([ProductAttributeMappingId] ASC, [DisplayOrder] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Product_ProductAttribute_Mapping_ProductId_DisplayOrder' AND object_id = OBJECT_ID(N'[Product_ProductAttribute_Mapping]')) DROP INDEX [IX_Product_ProductAttribute_Mapping_ProductId_DisplayOrder] ON [Product_ProductAttribute_Mapping]
GO
CREATE NONCLUSTERED INDEX [IX_Product_ProductAttribute_Mapping_ProductId_DisplayOrder] ON [Product_ProductAttribute_Mapping] ([ProductId] ASC, [DisplayOrder] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Manufacturer_DisplayOrder' AND object_id = OBJECT_ID(N'[Manufacturer]')) DROP INDEX [IX_Manufacturer_DisplayOrder] ON [Manufacturer]
GO
CREATE NONCLUSTERED INDEX [IX_Manufacturer_DisplayOrder] ON [Manufacturer] ([DisplayOrder] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Category_DisplayOrder' AND object_id = OBJECT_ID(N'[Category]')) DROP INDEX [IX_Category_DisplayOrder] ON [Category]
GO
CREATE NONCLUSTERED INDEX [IX_Category_DisplayOrder] ON [Category] ([DisplayOrder] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Category_ParentCategoryId' AND object_id = OBJECT_ID(N'[Category]')) DROP INDEX [IX_Category_ParentCategoryId] ON [Category]
GO
CREATE NONCLUSTERED INDEX [IX_Category_ParentCategoryId] ON [Category] ([ParentCategoryId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Forums_Group_DisplayOrder' AND object_id = OBJECT_ID(N'[Forums_Group]')) DROP INDEX [IX_Forums_Group_DisplayOrder] ON [Forums_Group]
GO
CREATE NONCLUSTERED INDEX [IX_Forums_Group_DisplayOrder] ON [Forums_Group] ([DisplayOrder] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Forums_Forum_DisplayOrder' AND object_id = OBJECT_ID(N'[Forums_Forum]')) DROP INDEX [IX_Forums_Forum_DisplayOrder] ON [Forums_Forum]
GO
CREATE NONCLUSTERED INDEX [IX_Forums_Forum_DisplayOrder] ON [Forums_Forum] ([DisplayOrder] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Forums_Forum_ForumGroupId' AND object_id = OBJECT_ID(N'[Forums_Forum]')) DROP INDEX [IX_Forums_Forum_ForumGroupId] ON [Forums_Forum]
GO
CREATE NONCLUSTERED INDEX [IX_Forums_Forum_ForumGroupId] ON [Forums_Forum] ([ForumGroupId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Forums_Topic_ForumId' AND object_id = OBJECT_ID(N'[Forums_Topic]')) DROP INDEX [IX_Forums_Topic_ForumId] ON [Forums_Topic]
GO
CREATE NONCLUSTERED INDEX [IX_Forums_Topic_ForumId] ON [Forums_Topic] ([ForumId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Forums_Post_TopicId' AND object_id = OBJECT_ID(N'[Forums_Post]')) DROP INDEX [IX_Forums_Post_TopicId] ON [Forums_Post]
GO
CREATE NONCLUSTERED INDEX [IX_Forums_Post_TopicId] ON [Forums_Post] ([TopicId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Forums_Post_CustomerId' AND object_id = OBJECT_ID(N'[Forums_Post]')) DROP INDEX [IX_Forums_Post_CustomerId] ON [Forums_Post]
GO
CREATE NONCLUSTERED INDEX [IX_Forums_Post_CustomerId] ON [Forums_Post] ([CustomerId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Forums_Subscription_ForumId' AND object_id = OBJECT_ID(N'[Forums_Subscription]')) DROP INDEX [IX_Forums_Subscription_ForumId] ON [Forums_Subscription]
GO
CREATE NONCLUSTERED INDEX [IX_Forums_Subscription_ForumId] ON [Forums_Subscription] ([ForumId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Forums_Subscription_TopicId' AND object_id = OBJECT_ID(N'[Forums_Subscription]')) DROP INDEX [IX_Forums_Subscription_TopicId] ON [Forums_Subscription]
GO
CREATE NONCLUSTERED INDEX [IX_Forums_Subscription_TopicId] ON [Forums_Subscription] ([TopicId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Product_Deleted_and_Published' AND object_id = OBJECT_ID(N'[Product]')) DROP INDEX [IX_Product_Deleted_and_Published] ON [Product]
GO
CREATE NONCLUSTERED INDEX [IX_Product_Deleted_and_Published] ON [Product] ([Published] ASC, [Deleted] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Product_Published' AND object_id = OBJECT_ID(N'[Product]')) DROP INDEX [IX_Product_Published] ON [Product]
GO
CREATE NONCLUSTERED INDEX [IX_Product_Published] ON [Product] ([Published] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Product_ShowOnHomepage' AND object_id = OBJECT_ID(N'[Product]')) DROP INDEX [IX_Product_ShowOnHomepage] ON [Product]
GO
CREATE NONCLUSTERED INDEX [IX_Product_ShowOnHomepage] ON [Product] ([ShowOnHomePage] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Product_ParentGroupedProductId' AND object_id = OBJECT_ID(N'[Product]')) DROP INDEX [IX_Product_ParentGroupedProductId] ON [Product]
GO
CREATE NONCLUSTERED INDEX [IX_Product_ParentGroupedProductId] ON [Product] ([ParentGroupedProductId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Product_VisibleIndividually' AND object_id = OBJECT_ID(N'[Product]')) DROP INDEX [IX_Product_VisibleIndividually] ON [Product]
GO
CREATE NONCLUSTERED INDEX [IX_Product_VisibleIndividually] ON [Product] ([VisibleIndividually] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PCM_Product_and_Category' AND object_id = OBJECT_ID(N'[Product_Category_Mapping]')) DROP INDEX [IX_PCM_Product_and_Category] ON [Product_Category_Mapping]
GO
CREATE NONCLUSTERED INDEX [IX_PCM_Product_and_Category] ON [Product_Category_Mapping] ([CategoryId] ASC, [ProductId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PMM_Product_and_Manufacturer' AND object_id = OBJECT_ID(N'[Product_Manufacturer_Mapping]')) DROP INDEX [IX_PMM_Product_and_Manufacturer] ON [Product_Manufacturer_Mapping]
GO
CREATE NONCLUSTERED INDEX [IX_PMM_Product_and_Manufacturer] ON [Product_Manufacturer_Mapping] ([ManufacturerId] ASC, [ProductId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PSAM_AllowFiltering' AND object_id = OBJECT_ID(N'[Product_SpecificationAttribute_Mapping]')) DROP INDEX [IX_PSAM_AllowFiltering] ON [Product_SpecificationAttribute_Mapping]
GO
CREATE NONCLUSTERED INDEX [IX_PSAM_AllowFiltering] ON [Product_SpecificationAttribute_Mapping] ([AllowFiltering] ASC) INCLUDE ([ProductId],[SpecificationAttributeOptionId])
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PSAM_SpecificationAttributeOptionId_AllowFiltering' AND object_id = OBJECT_ID(N'[Product_SpecificationAttribute_Mapping]')) DROP INDEX [IX_PSAM_SpecificationAttributeOptionId_AllowFiltering] ON [Product_SpecificationAttribute_Mapping]
GO
CREATE NONCLUSTERED INDEX [IX_PSAM_SpecificationAttributeOptionId_AllowFiltering] ON [Product_SpecificationAttribute_Mapping] ([SpecificationAttributeOptionId] ASC, [AllowFiltering] ASC) INCLUDE ([ProductId])
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PSAM_ProductId' AND object_id = OBJECT_ID(N'[Product_SpecificationAttribute_Mapping]')) DROP INDEX [IX_PSAM_ProductId] ON [Product_SpecificationAttribute_Mapping]
GO
CREATE NONCLUSTERED INDEX [IX_PSAM_ProductId] ON [Product_SpecificationAttribute_Mapping] ([ProductId] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProductTag_Name' AND object_id = OBJECT_ID(N'[ProductTag]')) DROP INDEX [IX_ProductTag_Name] ON [ProductTag]
GO
CREATE NONCLUSTERED INDEX [IX_ProductTag_Name] ON [ProductTag] ([Name] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ActivityLog_CreatedOnUtc' AND object_id = OBJECT_ID(N'[ActivityLog]')) DROP INDEX [IX_ActivityLog_CreatedOnUtc] ON [ActivityLog]
GO
CREATE NONCLUSTERED INDEX [IX_ActivityLog_CreatedOnUtc] ON [ActivityLog] ([CreatedOnUtc] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UrlRecord_Slug' AND object_id = OBJECT_ID(N'[UrlRecord]')) DROP INDEX [IX_UrlRecord_Slug] ON [UrlRecord]
GO
CREATE NONCLUSTERED INDEX [IX_UrlRecord_Slug] ON [UrlRecord] ([Slug] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UrlRecord_Custom_1' AND object_id = OBJECT_ID(N'[UrlRecord]')) DROP INDEX [IX_UrlRecord_Custom_1] ON [UrlRecord]
GO
CREATE NONCLUSTERED INDEX [IX_UrlRecord_Custom_1] ON [UrlRecord] ([EntityId] ASC, [EntityName] ASC, [LanguageId] ASC, [IsActive] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AclRecord_EntityId_EntityName' AND object_id = OBJECT_ID(N'[AclRecord]')) DROP INDEX [IX_AclRecord_EntityId_EntityName] ON [AclRecord]
GO
CREATE NONCLUSTERED INDEX [IX_AclRecord_EntityId_EntityName] ON [AclRecord] ([EntityId] ASC, [EntityName] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StoreMapping_EntityId_EntityName' AND object_id = OBJECT_ID(N'[StoreMapping]')) DROP INDEX [IX_StoreMapping_EntityId_EntityName] ON [StoreMapping]
GO
CREATE NONCLUSTERED INDEX [IX_StoreMapping_EntityId_EntityName] ON [StoreMapping] ([EntityId] ASC, [EntityName] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Category_LimitedToStores' AND object_id = OBJECT_ID(N'[Category]')) DROP INDEX [IX_Category_LimitedToStores] ON [Category]
GO
CREATE NONCLUSTERED INDEX [IX_Category_LimitedToStores] ON [Category] ([LimitedToStores] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Manufacturer_LimitedToStores' AND object_id = OBJECT_ID(N'[Manufacturer]')) DROP INDEX [IX_Manufacturer_LimitedToStores] ON [Manufacturer]
GO
CREATE NONCLUSTERED INDEX [IX_Manufacturer_LimitedToStores] ON [Manufacturer] ([LimitedToStores] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Product_LimitedToStores' AND object_id = OBJECT_ID(N'[Product]')) DROP INDEX [IX_Product_LimitedToStores] ON [Product]
GO
CREATE NONCLUSTERED INDEX [IX_Product_LimitedToStores] ON [Product] ([LimitedToStores] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Category_SubjectToAcl' AND object_id = OBJECT_ID(N'[Category]')) DROP INDEX [IX_Category_SubjectToAcl] ON [Category]
GO
CREATE NONCLUSTERED INDEX [IX_Category_SubjectToAcl] ON [Category] ([SubjectToAcl] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Manufacturer_SubjectToAcl' AND object_id = OBJECT_ID(N'[Manufacturer]')) DROP INDEX [IX_Manufacturer_SubjectToAcl] ON [Manufacturer]
GO
CREATE NONCLUSTERED INDEX [IX_Manufacturer_SubjectToAcl] ON [Manufacturer] ([SubjectToAcl] ASC)
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Product_SubjectToAcl' AND object_id = OBJECT_ID(N'[Product]')) DROP INDEX [IX_Product_SubjectToAcl] ON [Product]
GO
CREATE NONCLUSTERED INDEX [IX_Product_SubjectToAcl] ON [Product] ([SubjectToAcl] ASC)
GO