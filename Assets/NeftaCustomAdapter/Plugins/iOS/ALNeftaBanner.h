//
//  ALNeftaBanner.h
//  MaxIntegration
//
//  Created by Tomaz Treven on 3. 10. 24.
//

#import "ALNeftaAd.h"

@interface ALNeftaBanner : ALNeftaAd<NBannerListener>
@property NBanner * _Nonnull banner;
@property(nonatomic, weak) id<MAAdViewAdapterDelegate> listener;
- (instancetype _Nonnull) initWithId:(NSString *_Nonnull)aId listener:(id<MAAdViewAdapterDelegate>_Nonnull)listener;
+ (NSString * _Nullable) GetLastAuctionId;
+ (NSString * _Nullable) GetLastCreativeId;
@end
