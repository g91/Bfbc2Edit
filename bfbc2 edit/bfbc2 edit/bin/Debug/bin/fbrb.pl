# Author:       David Hwang
# Date:         2011/10/25
# Version:      1.0
# Purpose:      Generate a new fbrb from an old fbrb and a QuickBMS-extracted folder
# Requirements: 1. QuickBMS (to extract files from fbrb)
#               2. perl (to execute this script)
#               3. gzip (to compress files)
# Usage:        1. open command prompt
#               2. put perl.exe and gzip.exe in current folder or put them in your PATH
#               3. use QuickBMS to extract files to a folder
#               4. perl fbrb.pl OLD_FBRB FOLDER NEW_FBRB
# Thanks:       Luigi Auriemma for QuickBMS and his script for extracting files from fbrb

die "perl fbrb.pl OLD_FBRB FOLDER NEW_FBRB" if ($#ARGV  != 2);

$old_fbrb = $ARGV[0];
$folder   = $ARGV[1];
$new_fbrb = $ARGV[2];
$header   = "Header";
$filelist = "FileList";
$data     = "Data";

die "Please use a different filename for $new_fbrb or put it in a different folder" if ($old_fbrb eq $new_fbrb);
die "$folder is not a folder" if (! -d $folder);

Get_Header();
Gen_FileList();
Gen_Data();
Gen_Header();
Gen_fbrb();
CleanUp();

sub Get_Header {
   open(IN, "$old_fbrb")        || die "unable to open $old_fbrb";      
   open(OUT, ">$header.BAK.gz") || die "unable to open $header.BAK.gz";
   
   binmode(IN);
   binmode(OUT);
   
   read(IN, $_, 4);
   die "$old_fbrb is not a correct fbrb" if ($_ ne 'FbRB');
   
   read(IN, $_, 4);
   my $filesize = unpack('N', $_);
   
   read(IN, $_, $filesize);
   print OUT $_;
   
   close(IN);
   close(OUT);
   
   system("bin\\tools\\gzip -d -f $header.BAK.gz") == 0 || die "gzip error";        
}

sub Gen_FileList {
   open(IN, "$header.BAK") || die "unable to open $header.BAK";
   open(OUT, ">$filelist") || die "unable to open $filelist";
   
   binmode(IN);
   
   read(IN, $_, 4);
   
   read(IN, $_, 4);
   my $filesize = unpack('N', $_);
   
   read(IN, $_, $filesize);
   my @files = split(pack('C', 0), $_);
   
   for my $file (@files) {
      my $filepath = $folder . '/' . $file;
      if (-f $filepath) {
         print OUT "$file\n";
      }
   }
   
   close(IN);
   close(OUT);  
}

sub Gen_Data {
   open(IN, "$filelist") || die "unable to open $filelist";
   open(OUT, ">$data")   || die "unable to open $data";
   
   binmode(OUT);
   
   while(<IN>) {
      chop;
      my $file = $folder . '/' . $_;
      open(FILE, $file) || die "unable to open $file";
      binmode(FILE);
      while(read(FILE, $_, 0x100000)) {
         print OUT $_;
      }
      close(FILE);
   }
   
   close(IN);
   close(OUT);  
}

sub Gen_Header {
   my $dummy_size1 = 8;
   my $dummy_size2 = 12;
   my $dummy_size3 = 4;
   
   open(IN1, "$filelist")   || die "unable to open $filelist";
   open(IN2, "$header.BAK") || die "unable to open $header.BAK";
   open(OUT, ">$header")    || die "unable to open $header";
   
   binmode(IN2);
   binmode(OUT);
   
   my $filesize = (stat("$header.BAK"))[7];
   
   read(IN2, $_, $dummy_size3);
   read(IN2, $_, $dummy_size3);
   my $end_offset = unpack('N', $_);
   
   my $start_offset = $end_offset + 12;
   seek IN2, 0, SEEK_SET;                       # go to beginning
   
   read(IN2, $_, $start_offset);
   print OUT $_;
   
   my $offset = 0;
   while(<IN1>) {
      chop;
      my $file = $folder . '/' . $_;
      if (-f $file) {
         my $filesize = (stat($file))[7];
      
         read(IN2, $_, $dummy_size1);
         print OUT $_;   
      
         read(IN2, $_, $dummy_size2);
         print OUT pack('N3', $offset, $filesize, $filesize);
      
         read(IN2, $_, $dummy_size3);
         print OUT $_;
      
         $offset += $filesize;
      }
   }
   
   $filesize = (stat($data))[7];
   print OUT 1;
   print OUT pack('N', $filesize);
   
   close(IN1);
   close(IN2);
   close(OUT);          
}

sub Gen_fbrb {
   system("bin\\tools\\gzip -k -n -f $header") == 0 || die "gzip error";
   system("bin\\tools\\gzip -k -n -f $data")   == 0 || die "gzip error";
   
   open(IN1, "$header.gz") || die "unable to open $header.gz";
   open(IN2, "$data.gz")   || die "unable to open $data.gz";
   open(OUT, ">$new_fbrb") || die "unable to open $new_fbrb";
   
   binmode(IN1);
   binmode(IN2);
   binmode(OUT);
   
   my $compressed_headersize = (stat("$header.gz"))[7];
   
   print OUT "FbRB";
   print OUT pack('N', $compressed_headersize);
   
   while(read(IN1, $_, 0x10000)) {
      print OUT $_;
   }
   
   while(read(IN2, $_, 0x10000)) {
      print OUT $_;
   }
   
   close(IN1);
   close(IN2);
   close(OUT);   
}

sub CleanUp {
   unlink "$header", "$header.BAK", "$header.gz", "$filelist", "$data", "data.gz";
}
