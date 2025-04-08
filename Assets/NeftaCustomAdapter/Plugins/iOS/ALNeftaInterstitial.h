//
//  ALNeftaInterstitial.h
//  MaxIntegration
//
//  Created by Tomaz Treven on 3. 10. 24.
//

#import "ALNeftaAd.h"

@interface ALNeftaInterstitial : ALNeftaAd<NInterstitialListener>
@property NInterstitial * _Nonnull interstitial;
@property(nonatomic, weak) id<MAInterstitialAdapterDelegate> listener;
- (instancetype _Nonnull )initWithId:(NSString *_Nonnull)id listener:(id<MAInterstitialAdapterDelegate>_Nonnull)listener;
+ (NSString * _Nullable) GetLastAuctionId;
+ (NSString * _Nullable) GetLastCreativeId;
@end
