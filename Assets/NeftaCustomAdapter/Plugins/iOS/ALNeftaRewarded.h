//
//  ALNeftaRewarded.h
//  MaxIntegration
//
//  Created by Tomaz Treven on 3. 10. 24.
//

#import "ALNeftaAd.h"

@interface ALNeftaRewarded : ALNeftaAd<NRewardedListener>
@property NRewarded * _Nonnull rewarded;
@property MAReward * _Nullable reward;
@property bool shouldAlwaysReward;
@property bool giveReward;
@property(nonatomic, weak) id<MARewardedAdapterDelegate> listener;
- (instancetype _Nonnull)initWithId:(NSString *_Nonnull)id listener:(id<MARewardedAdapterDelegate>_Nonnull)listener;
@end
