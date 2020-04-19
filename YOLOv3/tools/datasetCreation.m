close all
clearvars
clc

global next

files = [dir('../data/EMSfIIoT')];
[m,~] = size(files);
idx = randperm(m);
files(idx,1) = files(:,1);

f = figure;
f.WindowState = 'maximized';
c = uicontrol;
c.String = 'Go Back';
c.Callback = @plotButtonPushed;

k = 1;
fileID = fopen('../data/EMSfIIoT/new/textfile.txt', 'a');
while k <= m
    next = true;
    fileDir = strcat(files(k).folder, '\', files(k).name);
    try
       im = imread(fileDir);
    catch
        k = k + 1;
       continue
    end
    fileDir = erase(fileDir,[pwd '\']);
    fileDir = strrep(fileDir,'\','/');
    
    if contains(fileDir,'false')
        disp('Processed image from false')
    else
        try
            imshow(im);
            answer = inputdlg('Insert number in counter');
            fprintf(fileID, '%s', fileDir);
            for i = 1:strlength(answer{1})
                number = answer{1}(i);
                title(['Identify number ', number, ' position'])
                roi = drawrectangle;
                
                if ~next
                    break
                end
                
                left = round(roi.Position(1));
                top = round(roi.Position(2));
                width = round(roi.Position(3));
                height = round(roi.Position(4));
                text(left+width/2, top-height/2, number, 'Color', 'Red', 'FontSize', 18)

                number = str2double(number);
                
                x_min = left;
                y_min = top;
                x_max = left + width;
                y_max = top + height;
                class_id = number;

                result = [x_min,y_min,x_max,y_max,class_id];
                fprintf(fileID, ' %d,%d,%d,%d,%d', result);
            end
            if next
                fprintf(fileID, '\n');
                title('Done!')
                pause(1)
            end
        catch
            close all
            break
        end
    end
    if next
        k = k + 1;
    end
end
fclose(fileID);

function plotButtonPushed(src,event)
    global next
    next = false;
end