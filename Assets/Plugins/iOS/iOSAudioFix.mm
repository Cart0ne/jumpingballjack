#import <AVFoundation/AVFoundation.h>

extern "C"
{
    void _SetAudioSessionPlayback()
    {
        AVAudioSession *session = [AVAudioSession sharedInstance];
        NSError *categoryError = nil;
        NSError *activationError = nil;

        [session setCategory:AVAudioSessionCategoryPlayback error:&categoryError];
        [session setActive:YES error:&activationError];

        if (categoryError) {
            NSLog(@"[iOSAudioFix] Error setting category: %@", categoryError.localizedDescription);
        }
        if (activationError) {
            NSLog(@"[iOSAudioFix] Error activating session: %@", activationError.localizedDescription);
        }
    }
}
