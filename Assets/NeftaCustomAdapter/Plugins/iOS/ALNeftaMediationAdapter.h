//
//  ALNeftaAdapter.h
//  NeftaMaxAdapter
//
//  Created by Tomaz Treven on 09/11/2023.
//

#ifndef ALNeftaAdapter_h
#define ALNeftaAdapter_h

#import "ALNeftaAd.h"

@interface ALNeftaMediationAdapter : ALMediationAdapter <MAAdViewAdapter, MAInterstitialAdapter, MARewardedAdapter>
typedef NS_ENUM(NSInteger, AdType) {
    AdTypeOther = 0,
    AdTypeBanner = 1,
    AdTypeInterstitial = 2,
    AdTypeRewarded = 3
};
@property (nonatomic, strong, readonly) NSString * _Nonnull _mediationProvider;
+ (void)OnExternalAdLoad:(AdType)adType unitFloorPrice:(double)unitFloorPrice calculatedFloorPrice:(double)calculatedFloorPrice;
+ (void)OnExternalAdFail:(AdType)adType unitFloorPrice:(double)unitFloorPrice calculatedFloorPrice:(double)calculatedFloorPrice error:(MAError * _Nonnull)error;
+ (void)OnExternalAdShown:(MAAd* _Nonnull)ad;
@property ALNeftaAd * _Nullable ad;
@end

#endif /* ALNeftaAdapter_h */
